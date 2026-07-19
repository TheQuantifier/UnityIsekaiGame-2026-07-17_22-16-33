using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.GameData.Persistence;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PrototypePersistenceServiceBehaviour : MonoBehaviour
    {
        [SerializeField] private PrototypePersistenceState prototypeState;
        [SerializeField] private string prototypeSlotId = PersistenceService.PrototypeSlotId;

        private PersistenceService service;
        private PrototypePersistenceStateParticipant participant;

        public PersistenceService Service => service;
        public PrototypePersistenceState PrototypeState => prototypeState;
        public string PrototypeSlotId => string.IsNullOrWhiteSpace(prototypeSlotId) ? PersistenceService.PrototypeSlotId : prototypeSlotId;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnDisable()
        {
            if (service != null && participant != null)
            {
                service.UnregisterParticipant(participant);
            }
        }

        public void EnsureInitialized()
        {
            if (prototypeState == null)
            {
                prototypeState = GetComponent<PrototypePersistenceState>();
            }

            if (prototypeState == null)
            {
                prototypeState = gameObject.AddComponent<PrototypePersistenceState>();
            }

            service ??= new PersistenceService();
            if (participant == null)
            {
                participant = new PrototypePersistenceStateParticipant(prototypeState);
                service.RegisterParticipant(participant, out string failureReason);
                if (!string.IsNullOrWhiteSpace(failureReason))
                {
                    Debug.LogWarning(failureReason);
                }
            }
        }

        public PersistenceSaveResult SavePrototypeSlot()
        {
            EnsureInitialized();
            PersistenceSaveResult result = service.Save(PrototypeSlotId, "Prototype Slot");
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceLoadResult LoadPrototypeSlot()
        {
            EnsureInitialized();
            PersistenceLoadResult result = service.Load(PrototypeSlotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceLoadResult LoadPrototypeBackup()
        {
            EnsureInitialized();
            PersistenceLoadResult result = service.Load(PrototypeSlotId, loadBackup: true);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceValidationResult ValidatePrototypeSlot()
        {
            EnsureInitialized();
            PersistenceValidationResult result = service.ValidateSlot(PrototypeSlotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public PersistenceDeleteResult DeletePrototypeSlot()
        {
            EnsureInitialized();
            PersistenceDeleteResult result = service.DeleteSlot(PrototypeSlotId);
            Report(result.Succeeded, result.Message);
            return result;
        }

        public IReadOnlyList<SaveSlotMetadata> ListSaveSlots()
        {
            EnsureInitialized();
            return service.ListSaveSlots();
        }

        private static void Report(bool succeeded, string message)
        {
            if (succeeded)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogWarning(message);
            }

            PrototypeHudMessageBus.Show(message);
        }
    }
}
