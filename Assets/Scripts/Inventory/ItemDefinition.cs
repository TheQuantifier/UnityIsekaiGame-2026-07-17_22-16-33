using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Inventory
{
    [CreateAssetMenu(fileName = "NewItemDefinition", menuName = "Unity Isekai Game/Inventory/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject, IInventoryItemDefinition, IUsableItemDefinition, IEquippableItemDefinition, IHasRarity, IItemInstancePolicy
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private CategoryDefinition primaryCategory;
        [SerializeField] private TagDefinition[] tags;
        [SerializeField] private RarityDefinition rarity;
        [SerializeField] private ItemInstanceMode instanceMode = ItemInstanceMode.DefinitionOnly;
        [SerializeField] private bool stackable = true;
        [SerializeField, Min(1)] private int maximumStackSize = 1;
        [SerializeField] private ItemUseEffect[] useEffects;
        [SerializeField] private EquipmentData equipment;

        public string ItemId => itemId;
        public string Id => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public CategoryDefinition PrimaryCategory => primaryCategory;
        public CategoryDomain ClassificationDomain => CategoryDomain.Item;
        public IReadOnlyList<TagDefinition> Tags => tags ?? System.Array.Empty<TagDefinition>();
        public RarityDefinition Rarity => rarity;
        public ItemInstanceMode InstanceMode => instanceMode;
        public bool Stackable => stackable;
        public int MaximumStackSize => stackable ? Mathf.Max(1, maximumStackSize) : 1;
        public IReadOnlyList<ItemUseEffect> UseEffects => useEffects;
        public bool IsUsable => useEffects != null && useEffects.Length > 0;
        public int UseEffectCount => useEffects == null ? 0 : useEffects.Length;
        public bool HasMissingUseEffect
        {
            get
            {
                if (useEffects == null)
                {
                    return false;
                }

                for (int i = 0; i < useEffects.Length; i++)
                {
                    if (useEffects[i] == null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        public EquipmentData Equipment => equipment;
        public bool IsEquippable => equipment != null && equipment.Equippable;

        private void OnValidate()
        {
            maximumStackSize = Mathf.Max(1, maximumStackSize);
            equipment?.Validate();
        }
    }
}
