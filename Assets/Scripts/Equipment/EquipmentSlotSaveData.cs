using System;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Equipment
{
    [Serializable]
    public sealed class EquipmentSlotSaveData
    {
        public EquipmentSlotType slotType;
        public EquipmentEntrySaveMode mode;
        public string definitionId;
        public ItemInstanceSaveData itemInstance;
    }
}
