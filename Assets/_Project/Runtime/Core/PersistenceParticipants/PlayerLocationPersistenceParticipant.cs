using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Input;
using UnityIsekaiGame.Places;
using UnityIsekaiGame.Player;

namespace UnityIsekaiGame.Persistence
{
    public sealed class PlayerLocationPersistenceParticipant : IPersistenceParticipant
    {
        public const int CurrentParticipantSchemaVersion = 1;
        public const string ParticipantKeyValue = "player.location";
        public const string SameSceneOnlyMode = "same-scene-v1";

        private const float MaximumCoordinateMagnitude = 10000f;
        private const float GroundProbeHeight = 2f;
        private const float GroundProbeDistance = 8f;

        private readonly Func<DefinitionRegistry> registryProvider;
        private readonly string ownerId;
        private readonly string sceneKey;
        private readonly string defaultSpawnPointId;
        private readonly Transform playerRoot;
        private readonly PlayerInputReader input;
        private readonly IPlayerMenuController inventoryScreenController;
        private readonly CurrentPlaceTracker placeTracker;

        public PlayerLocationPersistenceParticipant(
            Transform playerRoot,
            Func<DefinitionRegistry> registryProvider,
            string ownerId,
            string sceneKey,
            string defaultSpawnPointId,
            PlayerInputReader input = null,
            IPlayerMenuController inventoryScreenController = null,
            CurrentPlaceTracker placeTracker = null)
        {
            this.playerRoot = playerRoot;
            this.registryProvider = registryProvider;
            this.ownerId = string.IsNullOrWhiteSpace(ownerId) ? PersistenceService.LocalPlayerId : ownerId;
            this.sceneKey = string.IsNullOrWhiteSpace(sceneKey) ? ResolveLoadedSceneKey() : sceneKey;
            this.defaultSpawnPointId = string.IsNullOrWhiteSpace(defaultSpawnPointId) ? "spawn.prototype.default" : defaultSpawnPointId;
            this.input = input;
            this.inventoryScreenController = inventoryScreenController;
            this.placeTracker = placeTracker;
        }

        public event Action<LocationRestoreEventArgs> LocationRestoreStarted;
        public event Action<LocationRestoreEventArgs> LocationRestoreCompleted;
        public event Action<LocationFallbackEventArgs> LocationFallbackUsed;

        public string ParticipantKey => ParticipantKeyValue;
        public int ParticipantSchemaVersion => CurrentParticipantSchemaVersion;
        public bool IsRequired => false;
        public PersistenceScope Scope => PersistenceScope.Player;
        public string OwnerId => ownerId;
        public PersistenceLoadPhase LoadPhase => PersistenceLoadPhase.PositionAndPlace;
        public int LoadPriority => 0;

        public PersistenceParticipantSaveResult CapturePayload()
        {
            if (playerRoot == null)
            {
                return PersistenceParticipantSaveResult.Failure("Player location capture failed because player root is missing.");
            }

            Vector3 position = playerRoot.position;
            Quaternion rotation = Normalize(playerRoot.rotation);
            if (!IsFinite(position))
            {
                return PersistenceParticipantSaveResult.Failure("Player location capture failed because player position is not finite.");
            }

            if (!IsValidRotation(rotation))
            {
                return PersistenceParticipantSaveResult.Failure("Player location capture failed because player rotation is invalid.");
            }

            PlayerLocationSaveData saveData = new PlayerLocationSaveData
            {
                schemaVersion = CurrentParticipantSchemaVersion,
                sceneKey = sceneKey,
                sceneBuildIndex = SceneManager.GetActiveScene().buildIndex,
                diagnosticSceneName = SceneManager.GetActiveScene().name,
                placeId = placeTracker == null ? string.Empty : placeTracker.CurrentPlaceId,
                positionX = position.x,
                positionY = position.y,
                positionZ = position.z,
                rotationX = rotation.x,
                rotationY = rotation.y,
                rotationZ = rotation.z,
                rotationW = rotation.w,
                spawnPointId = defaultSpawnPointId,
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                locationMode = SameSceneOnlyMode
            };

            PersistenceParticipantPrepareResult validation = PreparePayload(JsonUtility.ToJson(saveData), CurrentParticipantSchemaVersion);
            if (validation == null || !validation.Succeeded)
            {
                return PersistenceParticipantSaveResult.Failure(validation?.Message ?? "Player location snapshot failed validation.");
            }

            DiscardPreparedPayload(validation.PreparedPayload);
            return PersistenceParticipantSaveResult.Success(JsonUtility.ToJson(saveData));
        }

        public PersistenceParticipantPrepareResult PreparePayload(string payloadJson, int payloadSchemaVersion)
        {
            if (payloadSchemaVersion != CurrentParticipantSchemaVersion)
            {
                return PersistenceParticipantPrepareResult.Failure($"Unsupported player location participant schema version {payloadSchemaVersion}.");
            }

            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return PersistenceParticipantPrepareResult.Failure("Player location payload is empty.");
            }

            PlayerLocationSaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<PlayerLocationSaveData>(payloadJson);
            }
            catch (Exception)
            {
                return PersistenceParticipantPrepareResult.Failure("Player location payload is malformed JSON.");
            }

            if (saveData == null)
            {
                return PersistenceParticipantPrepareResult.Failure("Player location payload did not parse.");
            }

            if (saveData.schemaVersion != CurrentParticipantSchemaVersion)
            {
                return PersistenceParticipantPrepareResult.Failure($"Unsupported player location payload schema version {saveData.schemaVersion}.");
            }

            if (string.IsNullOrWhiteSpace(saveData.sceneKey))
            {
                return PersistenceParticipantPrepareResult.Failure("Player location payload is missing a scene key.");
            }

            if (!ValidateLoadedSceneKeys(out string sceneKeyFailure))
            {
                return PersistenceParticipantPrepareResult.Failure(sceneKeyFailure);
            }

            if (!string.Equals(saveData.sceneKey, sceneKey, StringComparison.Ordinal))
            {
                return PersistenceParticipantPrepareResult.Failure($"Cross-scene restore from '{saveData.sceneKey}' to loaded scene '{sceneKey}' is not supported by Feature 4.5.");
            }

            Vector3 savedPosition = new Vector3(saveData.positionX, saveData.positionY, saveData.positionZ);
            Quaternion savedRotation = new Quaternion(saveData.rotationX, saveData.rotationY, saveData.rotationZ, saveData.rotationW);
            if (!IsFinite(savedPosition))
            {
                return PersistenceParticipantPrepareResult.Failure("Player location payload contains a non-finite position.");
            }

            if (Mathf.Abs(savedPosition.x) > MaximumCoordinateMagnitude
                || Mathf.Abs(savedPosition.y) > MaximumCoordinateMagnitude
                || Mathf.Abs(savedPosition.z) > MaximumCoordinateMagnitude)
            {
                return PersistenceParticipantPrepareResult.Failure("Player location payload position is outside the supported prototype bounds.");
            }

            if (!IsValidRotation(savedRotation))
            {
                return PersistenceParticipantPrepareResult.Failure("Player location payload contains an invalid rotation.");
            }

            DefinitionRegistry registry = registryProvider?.Invoke();
            PlaceDefinition place = null;
            if (!string.IsNullOrWhiteSpace(saveData.placeId))
            {
                if (registry == null || !registry.DefinitionsById.TryGetValue(saveData.placeId, out IGameDefinition definition) || definition is not PlaceDefinition resolvedPlace)
                {
                    return PersistenceParticipantPrepareResult.Failure($"Player location payload references unknown place '{saveData.placeId}'.");
                }

                if (!string.IsNullOrWhiteSpace(resolvedPlace.SceneKey) && !string.Equals(resolvedPlace.SceneKey, saveData.sceneKey, StringComparison.Ordinal))
                {
                    return PersistenceParticipantPrepareResult.Failure($"Player location place '{resolvedPlace.Id}' belongs to scene '{resolvedPlace.SceneKey}', not saved scene '{saveData.sceneKey}'.");
                }

                place = resolvedPlace;
            }

            PlayerSpawnPoint spawn = ResolveSpawn(saveData.spawnPointId, place) ?? ResolveSpawn(defaultSpawnPointId, place) ?? ResolveAnySpawn(place);
            bool useFallback = !IsPositionSafe(savedPosition);
            if (useFallback && spawn == null)
            {
                return PersistenceParticipantPrepareResult.Failure("Saved player position is unsafe and no fallback spawn point is available.");
            }

            Vector3 targetPosition = useFallback ? spawn.transform.position : savedPosition;
            Quaternion targetRotation = useFallback ? Normalize(spawn.transform.rotation) : Normalize(savedRotation);
            if (!IsFinite(targetPosition) || !IsValidRotation(targetRotation))
            {
                return PersistenceParticipantPrepareResult.Failure("Resolved player location target is invalid.");
            }

            return PersistenceParticipantPrepareResult.Success(new PreparedPlayerLocation(saveData, place, targetPosition, targetRotation, useFallback, spawn));
        }

        public PersistenceParticipantCommitResult CommitPreparedPayload(object preparedPayload)
        {
            if (playerRoot == null)
            {
                return PersistenceParticipantCommitResult.Failure("Player location commit failed because player root is missing.");
            }

            if (preparedPayload is not PreparedPlayerLocation prepared)
            {
                return PersistenceParticipantCommitResult.Failure("Prepared player location payload has the wrong type.");
            }

            Vector3 previousPosition = playerRoot.position;
            Quaternion previousRotation = playerRoot.rotation;
            CharacterController characterController = playerRoot.GetComponent<CharacterController>();
            bool wasControllerEnabled = characterController != null && characterController.enabled;

            LocationRestoreStarted?.Invoke(new LocationRestoreEventArgs(prepared.SaveData.sceneKey, prepared.Place, prepared.TargetPosition, prepared.TargetRotation, prepared.FallbackUsed));

            using (LocationRestoreGuard.Enter())
            {
                try
                {
                    input?.SetGameplayInputBlocked(true);
                    input?.ClearGameplayActionQueues();
                    inventoryScreenController?.CloseForPrototypeReset();

                    if (characterController != null)
                    {
                        characterController.enabled = false;
                    }

                    playerRoot.SetPositionAndRotation(prepared.TargetPosition, prepared.TargetRotation);
                    playerRoot.GetComponent<FirstPersonCharacterMotor>()?.ResetTransientMotionForPersistenceRestore();
                    foreach (FirstPersonCameraLook cameraLook in playerRoot.GetComponentsInChildren<FirstPersonCameraLook>(true))
                    {
                        cameraLook.SyncToCurrentRotationForPersistenceRestore();
                    }

                    if (characterController != null)
                    {
                        characterController.enabled = wasControllerEnabled;
                    }

                    placeTracker?.ForceCurrentPlace(prepared.Place, isRestoration: true);
                    input?.ClearGameplayActionQueues();
                    input?.SetGameplayInputBlocked(false);

                    if (prepared.FallbackUsed)
                    {
                        LocationFallbackUsed?.Invoke(new LocationFallbackEventArgs(prepared.SaveData.sceneKey, prepared.SaveData.placeId, prepared.TargetPosition, prepared.SpawnPoint == null ? string.Empty : prepared.SpawnPoint.SpawnPointId, "Saved position was unsafe; used fallback spawn."));
                    }

                    LocationRestoreCompleted?.Invoke(new LocationRestoreEventArgs(prepared.SaveData.sceneKey, prepared.Place, prepared.TargetPosition, prepared.TargetRotation, prepared.FallbackUsed));
                    return PersistenceParticipantCommitResult.Success(prepared.FallbackUsed
                        ? "Player location restored using a safe fallback spawn."
                        : "Player location restored.");
                }
                catch (Exception exception)
                {
                    if (characterController != null)
                    {
                        characterController.enabled = false;
                    }

                    playerRoot.SetPositionAndRotation(previousPosition, previousRotation);
                    if (characterController != null)
                    {
                        characterController.enabled = wasControllerEnabled;
                    }

                    input?.SetGameplayInputBlocked(false);
                    return PersistenceParticipantCommitResult.Failure($"Player location commit failed; previous transform restored: {exception.Message}");
                }
            }
        }

        public void DiscardPreparedPayload(object preparedPayload)
        {
        }

        public string BuildDiagnosticSummary()
        {
            if (playerRoot == null)
            {
                return $"Scene: {sceneKey}\nPlayer location: missing player root";
            }

            Vector3 p = playerRoot.position;
            Quaternion r = playerRoot.rotation;
            string place = placeTracker == null || placeTracker.CurrentPlace == null
                ? "None"
                : $"{placeTracker.CurrentPlace.DisplayName} ({placeTracker.CurrentPlace.Id})";
            return $"Scene: {sceneKey}\nPlace: {place}\nPosition: {p.x:0.00}, {p.y:0.00}, {p.z:0.00}\nRotation: {r.x:0.###}, {r.y:0.###}, {r.z:0.###}, {r.w:0.###}";
        }

        private PlayerSpawnPoint ResolveSpawn(string spawnPointId, PlaceDefinition place)
        {
            if (string.IsNullOrWhiteSpace(spawnPointId))
            {
                return null;
            }

            return UnityEngine.Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Exclude)
                .Where(spawn => spawn != null && string.Equals(spawn.SpawnPointId, spawnPointId, StringComparison.Ordinal))
                .OrderByDescending(spawn => IsPlaceCompatible(spawn.Place, place))
                .ThenByDescending(spawn => spawn.Priority)
                .FirstOrDefault();
        }

        private PlayerSpawnPoint ResolveAnySpawn(PlaceDefinition place)
        {
            return UnityEngine.Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Exclude)
                .Where(spawn => spawn != null && (place == null || IsPlaceCompatible(spawn.Place, place)))
                .OrderByDescending(spawn => spawn.Priority)
                .ThenBy(spawn => spawn.SpawnPointId, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private static bool IsPlaceCompatible(PlaceDefinition spawnPlace, PlaceDefinition savedPlace)
        {
            return savedPlace == null
                || spawnPlace == null
                || PlaceHierarchyUtility.ContainsOrIs(savedPlace, spawnPlace)
                || PlaceHierarchyUtility.ContainsOrIs(spawnPlace, savedPlace);
        }

        private static bool IsPositionSafe(Vector3 position)
        {
            if (!IsFinite(position))
            {
                return false;
            }

            if (!Physics.Raycast(position + Vector3.up * GroundProbeHeight, Vector3.down, GroundProbeHeight + GroundProbeDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return !Physics.CheckSphere(position + Vector3.up * 0.9f, 0.35f, ~0, QueryTriggerInteraction.Ignore);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsValidRotation(Quaternion value)
        {
            float magnitude = value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w) && magnitude > 0.0001f;
        }

        private static Quaternion Normalize(Quaternion value)
        {
            float magnitude = Mathf.Sqrt(value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w);
            if (magnitude <= 0.0001f)
            {
                return Quaternion.identity;
            }

            return new Quaternion(value.x / magnitude, value.y / magnitude, value.z / magnitude, value.w / magnitude);
        }

        private static string ResolveLoadedSceneKey()
        {
            SceneKeyIdentity[] identities = UnityEngine.Object.FindObjectsByType<SceneKeyIdentity>(FindObjectsInactive.Exclude);
            SceneKeyIdentity first = identities.FirstOrDefault(identity => identity != null && !string.IsNullOrWhiteSpace(identity.SceneKey));
            return first == null ? "scene.prototype" : first.SceneKey;
        }

        private static bool ValidateLoadedSceneKeys(out string failureReason)
        {
            failureReason = string.Empty;
            SceneKeyIdentity[] identities = UnityEngine.Object.FindObjectsByType<SceneKeyIdentity>(FindObjectsInactive.Exclude);
            int keyedCount = identities.Count(identity => identity != null && !string.IsNullOrWhiteSpace(identity.SceneKey));
            int distinctCount = identities
                .Where(identity => identity != null && !string.IsNullOrWhiteSpace(identity.SceneKey))
                .Select(identity => identity.SceneKey)
                .Distinct(StringComparer.Ordinal)
                .Count();
            if (keyedCount > distinctCount)
            {
                failureReason = "Duplicate loaded scene keys were detected.";
                return false;
            }

            return true;
        }

        private sealed class PreparedPlayerLocation
        {
            public PreparedPlayerLocation(PlayerLocationSaveData saveData, PlaceDefinition place, Vector3 targetPosition, Quaternion targetRotation, bool fallbackUsed, PlayerSpawnPoint spawnPoint)
            {
                SaveData = saveData;
                Place = place;
                TargetPosition = targetPosition;
                TargetRotation = targetRotation;
                FallbackUsed = fallbackUsed;
                SpawnPoint = spawnPoint;
            }

            public PlayerLocationSaveData SaveData { get; }
            public PlaceDefinition Place { get; }
            public Vector3 TargetPosition { get; }
            public Quaternion TargetRotation { get; }
            public bool FallbackUsed { get; }
            public PlayerSpawnPoint SpawnPoint { get; }
        }
    }

    public sealed class LocationRestoreEventArgs : EventArgs
    {
        public LocationRestoreEventArgs(string sceneKey, PlaceDefinition place, Vector3 position, Quaternion rotation, bool fallbackUsed)
        {
            SceneKey = sceneKey;
            Place = place;
            Position = position;
            Rotation = rotation;
            FallbackUsed = fallbackUsed;
        }

        public string SceneKey { get; }
        public PlaceDefinition Place { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public bool FallbackUsed { get; }
    }

    public sealed class LocationFallbackEventArgs : EventArgs
    {
        public LocationFallbackEventArgs(string sceneKey, string placeId, Vector3 fallbackPosition, string spawnPointId, string message)
        {
            SceneKey = sceneKey;
            PlaceId = placeId;
            FallbackPosition = fallbackPosition;
            SpawnPointId = spawnPointId;
            Message = message;
        }

        public string SceneKey { get; }
        public string PlaceId { get; }
        public Vector3 FallbackPosition { get; }
        public string SpawnPointId { get; }
        public string Message { get; }
    }
}
