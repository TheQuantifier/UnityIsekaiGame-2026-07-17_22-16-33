using UnityEngine;

namespace UnityIsekaiGame.Inventory
{
    [CreateAssetMenu(fileName = "NewItemDefinition", menuName = "Unity Isekai Game/Inventory/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private bool stackable = true;
        [SerializeField, Min(1)] private int maximumStackSize = 1;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public bool Stackable => stackable;
        public int MaximumStackSize => stackable ? Mathf.Max(1, maximumStackSize) : 1;

        private void OnValidate()
        {
            maximumStackSize = Mathf.Max(1, maximumStackSize);
        }
    }
}
