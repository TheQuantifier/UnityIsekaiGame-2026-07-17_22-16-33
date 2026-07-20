namespace UnityIsekaiGame.GameData
{
    public static class ItemInstanceFactory
    {
        public static ItemInstanceCreationResult CreateDefinitionOnly(IInventoryItemDefinition definition)
        {
            return Create(definition, null, null, requirePersistentIdentity: false);
        }

        public static ItemInstanceCreationResult CreateStateful(IInventoryItemDefinition definition, ItemInstanceMetadata metadata, string instanceId = null)
        {
            return Create(definition, metadata, string.IsNullOrWhiteSpace(instanceId) ? ItemInstanceId.Generate() : instanceId, requirePersistentIdentity: true);
        }

        public static ItemInstanceCreationResult Create(
            IInventoryItemDefinition definition,
            ItemInstanceMetadata metadata,
            string instanceId,
            bool requirePersistentIdentity)
        {
            if (definition == null)
            {
                return ItemInstanceCreationResult.Failure(ItemInstanceCreationStatus.MissingDefinition, "Cannot create an item instance without an item definition.");
            }

            if (!ItemInstanceId.IsValid(instanceId))
            {
                return ItemInstanceCreationResult.Failure(ItemInstanceCreationStatus.InvalidInstanceId, $"Item instance ID '{instanceId}' is not a canonical GUID string.");
            }

            if (requirePersistentIdentity && string.IsNullOrWhiteSpace(instanceId))
            {
                return ItemInstanceCreationResult.Failure(ItemInstanceCreationStatus.MissingRequiredInstanceId, "Stateful item instances require a persistent instance ID.");
            }

            return ItemInstanceCreationResult.Success(new ItemInstance(definition, instanceId, metadata));
        }
    }
}
