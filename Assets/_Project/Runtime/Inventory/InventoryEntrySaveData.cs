using System;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Inventory
{
    [Serializable]
    public sealed class InventoryEntrySaveData
    {
        public InventoryEntrySaveMode mode;
        public string definitionId;
        public int quantity;
        public ItemInstanceSaveData itemInstance;
    }
}
