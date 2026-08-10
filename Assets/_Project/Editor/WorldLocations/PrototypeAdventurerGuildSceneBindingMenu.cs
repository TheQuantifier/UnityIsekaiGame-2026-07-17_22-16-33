#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityIsekaiGame.GameData.Persistence;
using UnityIsekaiGame.WorldLocations;
using UnityIsekaiGame.WorldLocations.SceneBinding;

namespace UnityIsekaiGame.Editor.WorldLocations
{
    public static class PrototypeAdventurerGuildSceneBindingMenu
    {
        private const string PrototypeScenePath = "Assets/_Project/Scenes/Prototype/PrototypeScene.unity";
        private const string PrototypeSceneKey = "scene.prototype";

        [MenuItem("Tools/World Locations/Prototype Scene/Adventurer Guild/Apply Step 14 Scene Bindings")]
        public static void ApplyToPrototypeSceneMenu()
        {
            int sceneChanges = ApplyToPrototypeScene();
            Debug.Log($"Applied Adventurer Guild Step 14 scene bindings to PrototypeScene. Scene changes={sceneChanges}.");
        }

        public static void ApplyToPrefabAndScene()
        {
            ApplyToPrototypeSceneMenu();
        }

        public static int ApplyToPrototypeScene()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
            GameObject guild = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject)
                .FirstOrDefault(item => string.Equals(item.name, "AdventurerGuild", StringComparison.Ordinal));

            if (guild == null)
            {
                Debug.LogWarning("Prototype scene AdventurerGuild instance was not found; prefab bindings were still prepared.");
                return 0;
            }

            int changes = ApplyBindings(guild);
            if (changes > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            return changes;
        }

        [MenuItem("Tools/World Locations/Prototype Scene/Adventurer Guild/Validate Step 14 Scene Bindings")]
        public static void ValidatePrototypeSceneBindings()
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
            WorldSceneBindingValidationReport report = WorldSceneBindingValidationMenu.ValidateBindings(UnityEngine.Object.FindObjectsByType<WorldSceneBindingComponent>(FindObjectsInactive.Include));
            string details = report.Issues.Count == 0
                ? "No Adventurer Guild scene binding issues found."
                : string.Join(Environment.NewLine, report.Issues.Select(issue => issue.ToString()));

            string message = $"Prototype scene binding validation finished for '{scene.path}'. {report.Summary}{Environment.NewLine}{details}";
            if (report.Succeeded)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogWarning(message);
            }
        }

        private static int ApplyBindings(GameObject guildRoot)
        {
            if (guildRoot == null)
            {
                return 0;
            }

            int changes = 0;
            changes += EnsureBootstrap(guildRoot);
            changes += EnsureLocation(guildRoot, "location.prototype.adventurers-guild", "prototype.scene.location.adventurers-guild", PrototypeLocationDefinitionFactory.GuildHallDefinitionId, required: true);

            changes += EnsureLocationMarker(guildRoot, "Interact - Adventurer Guild Counter", "location.prototype.adventurers-guild", "prototype.scene.location.adventurers-guild.counter-area", PrototypeLocationDefinitionFactory.GuildHallDefinitionId);
            changes += EnsureInteraction(guildRoot, "Interact - Adventurer Guild Counter", PrototypeInteractionPointDefinitionFactory.AdventurerGuildCounterPointId, "prototype.scene.interaction.adventurer-guild-counter");

            changes += EnsureLocationMarker(guildRoot, "Interact - Merchant Guild Counter", "location.prototype.merchant-counter", "prototype.scene.location.merchant-counter", PrototypeLocationDefinitionFactory.MarketStallDefinitionId);
            changes += EnsureInteraction(guildRoot, "Interact - Merchant Guild Counter", PrototypeInteractionPointDefinitionFactory.MerchantGuildCounterPointId, "prototype.scene.interaction.merchant-guild-counter");

            changes += EnsureLocationMarker(guildRoot, "Interact - City Office Records Desk", "location.prototype.civic-office", "prototype.scene.location.civic-office.records-desk", PrototypeLocationDefinitionFactory.GovernmentBuildingDefinitionId);
            changes += EnsureInteraction(guildRoot, "Interact - City Office Records Desk", PrototypeInteractionPointDefinitionFactory.RecordsDeskPointId, "prototype.scene.interaction.city-records-desk");

            changes += EnsureLocationMarker(guildRoot, "Interact - Mayor Desk", "location.prototype.mayor-office", "prototype.scene.location.mayor-office", PrototypeLocationDefinitionFactory.OfficeDefinitionId);
            changes += EnsureInteraction(guildRoot, "Interact - Mayor Desk", PrototypeInteractionPointDefinitionFactory.MayorDeskPointId, "prototype.scene.interaction.mayor-desk");

            changes += EnsureLocationMarker(guildRoot, "Interact - Guild Head Desk", "location.prototype.guildmaster-office", "prototype.scene.location.guild-head-office", PrototypeLocationDefinitionFactory.OfficeDefinitionId);
            changes += EnsureInteraction(guildRoot, "Interact - Guild Head Desk", PrototypeInteractionPointDefinitionFactory.GuildHeadDeskPointId, "prototype.scene.interaction.guild-head-desk");

            changes += EnsureLocationMarker(guildRoot, "Interact - Prison Cell A Door", "location.prototype.basement-prison", "prototype.scene.location.basement-prison", PrototypeLocationDefinitionFactory.DetentionAreaDefinitionId);
            changes += EnsureInteraction(guildRoot, "Interact - Prison Cell A Door", PrototypeInteractionPointDefinitionFactory.PrisonCellPointId, "prototype.scene.interaction.prison-cell");

            return changes;
        }

        private static int EnsureBootstrap(GameObject root)
        {
            RemoveMissingScripts(root);
            return EnsureComponent<WorldSceneBindingBootstrap>(root) ? 1 : 0;
        }

        private static int EnsureLocation(GameObject root, string locationId, string bindingKey, string definitionId, bool required, WorldSceneBindingRole role = WorldSceneBindingRole.Primary)
        {
            LocationSceneBinding binding = GetOrAdd<LocationSceneBinding>(root, out bool added);
            bool changed = added
                || binding.LogicalId != locationId
                || binding.BindingKey != bindingKey
                || binding.SceneKey != PrototypeSceneKey
                || binding.WorldId != PersistenceService.LocalWorldId
                || binding.LocationDefinitionId != definitionId
                || binding.Required != required
                || binding.Role != role;

            if (changed)
            {
                binding.ConfigureLocation(locationId, bindingKey, PrototypeSceneKey, PersistenceService.LocalWorldId, definitionId, role, required);
                EditorUtility.SetDirty(binding);
            }

            return changed ? 1 : 0;
        }

        private static int EnsureLocationMarker(GameObject root, string markerName, string locationId, string bindingKey, string definitionId)
        {
            GameObject marker = FindChild(root, markerName);
            return marker == null ? 0 : EnsureLocation(marker, locationId, bindingKey, definitionId, required: false, role: WorldSceneBindingRole.Auxiliary);
        }

        private static int EnsureInteraction(GameObject root, string markerName, string pointId, string bindingKey)
        {
            GameObject marker = FindChild(root, markerName);
            if (marker == null)
            {
                Debug.LogWarning($"Adventurer Guild interaction marker '{markerName}' was not found.");
                return 0;
            }

            InteractionPointSceneBinding binding = GetOrAdd<InteractionPointSceneBinding>(marker, out bool added);
            bool changed = added
                || binding.LogicalId != pointId
                || binding.BindingKey != bindingKey
                || binding.SceneKey != PrototypeSceneKey
                || binding.WorldId != PersistenceService.LocalWorldId
                || binding.Required;

            if (changed)
            {
                binding.ConfigureBinding(pointId, bindingKey, PrototypeSceneKey, PersistenceService.LocalWorldId, WorldSceneBindingRole.Primary, requiredBinding: false);
                EditorUtility.SetDirty(binding);
            }

            return changed ? 1 : 0;
        }

        private static bool EnsureComponent<T>(GameObject target) where T : Component
        {
            RemoveMissingScripts(target);
            RemoveDuplicateComponents<T>(target);
            if (target.GetComponent<T>() != null)
            {
                return false;
            }

            target.AddComponent<T>();
            EditorUtility.SetDirty(target);
            return true;
        }

        private static T GetOrAdd<T>(GameObject target, out bool added) where T : Component
        {
            RemoveMissingScripts(target);
            RemoveDuplicateComponents<T>(target);
            T component = target.GetComponent<T>();
            if (component != null)
            {
                added = false;
                return component;
            }

            added = true;
            component = target.AddComponent<T>();
            EditorUtility.SetDirty(target);
            return component;
        }

        private static void RemoveMissingScripts(GameObject target)
        {
            if (target != null)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
            }
        }

        private static void RemoveDuplicateComponents<T>(GameObject target) where T : Component
        {
            T[] components = target.GetComponents<T>();
            for (int i = components.Length - 1; i >= 1; i--)
            {
                UnityEngine.Object.DestroyImmediate(components[i], true);
                EditorUtility.SetDirty(target);
            }
        }

        private static GameObject FindChild(GameObject root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Select(transform => transform.gameObject)
                .FirstOrDefault(item => string.Equals(item.name, name, StringComparison.Ordinal));
        }
    }
}
#endif
