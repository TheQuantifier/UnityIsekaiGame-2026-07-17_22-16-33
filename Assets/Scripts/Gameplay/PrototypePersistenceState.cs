using UnityEngine;
using UnityIsekaiGame.GameData.Persistence;

namespace UnityIsekaiGame.Gameplay
{
    public sealed class PrototypePersistenceState : MonoBehaviour, IPrototypePersistenceState
    {
        [SerializeField] private int testValue;
        [SerializeField] private string note = "Prototype persistence note";
        [SerializeField] private bool flag;

        public int TestValue => testValue;
        public string Note => note;
        public bool Flag => flag;

        public void SetValue(int value)
        {
            testValue = value;
            Debug.Log($"Prototype persistence value set to {testValue}.");
            PrototypeHudMessageBus.Show($"Persistence value: {testValue}");
        }

        public void IncrementValue()
        {
            SetValue(testValue + 1);
        }

        public void SetNote(string value)
        {
            note = value ?? string.Empty;
        }

        public void ToggleFlag()
        {
            flag = !flag;
            Debug.Log($"Prototype persistence flag set to {flag}.");
        }

        public PrototypePersistenceStateSaveData CreateSaveData()
        {
            return new PrototypePersistenceStateSaveData
            {
                testValue = testValue,
                note = note,
                flag = flag
            };
        }

        public void RestoreFromSaveData(PrototypePersistenceStateSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            testValue = saveData.testValue;
            note = saveData.note ?? string.Empty;
            flag = saveData.flag;
            Debug.Log($"Prototype persistence state restored. Value={testValue}, Flag={flag}, Note='{note}'.");
            PrototypeHudMessageBus.Show($"Loaded persistence value: {testValue}");
        }
    }
}
