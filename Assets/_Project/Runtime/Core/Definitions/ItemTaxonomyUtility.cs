namespace UnityIsekaiGame.GameData
{
    public static class ItemTaxonomyUtility
    {
        public const string ItemCategoryId = "item";
        public const string EquipmentCategoryId = "item.equipment";
        public const string WeaponCategoryId = "item.weapon";
        public const string ArmorCategoryId = "item.armor";
        public const string ConsumableCategoryId = "item.consumable";
        public const string MaterialCategoryId = "item.material";
        public const string IngredientCategoryId = "item.ingredient";
        public const string ToolCategoryId = "item.tool";
        public const string TradeGoodCategoryId = "item.trade-good";
        public const string KeyCategoryId = "item.key";
        public const string QuestItemCategoryId = "item.quest-item";
        public const string BookCategoryId = "item.book";
        public const string MiscellaneousCategoryId = "item.miscellaneous";

        public static bool IsItemDefinition(IGameDefinition definition)
        {
            return definition is IInventoryItemDefinition;
        }

        public static bool IsWeapon(IInventoryItemDefinition item)
        {
            return IsInItemCategory(item, WeaponCategoryId);
        }

        public static bool IsArmor(IInventoryItemDefinition item)
        {
            return IsInItemCategory(item, ArmorCategoryId);
        }

        public static bool IsEquipment(IInventoryItemDefinition item)
        {
            return IsInItemCategory(item, EquipmentCategoryId);
        }

        public static bool IsConsumable(IInventoryItemDefinition item)
        {
            return IsInItemCategory(item, ConsumableCategoryId);
        }

        public static bool IsInItemCategory(IInventoryItemDefinition item, string categoryId)
        {
            return item != null && ClassificationUtility.IsInCategory(item, categoryId);
        }

        public static bool HasTag(IInventoryItemDefinition item, string tagId)
        {
            return item != null && ClassificationUtility.HasTag(item, tagId);
        }

        public static bool HasUseCapability(IGameDefinition definition)
        {
            return definition is IUsableItemDefinition usable && usable.IsUsable;
        }

        public static bool HasEquipCapability(IGameDefinition definition)
        {
            return definition is IEquippableItemDefinition equippable && equippable.IsEquippable;
        }
    }
}
