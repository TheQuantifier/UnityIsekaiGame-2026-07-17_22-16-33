using System;

namespace UnityIsekaiGame.GameData
{
    public sealed class ItemInstance
    {
        public ItemInstance(IInventoryItemDefinition definition, string instanceId = null, ItemInstanceMetadata metadata = null)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));

            if (!ItemInstanceId.IsValid(instanceId))
            {
                throw new ArgumentException("Item instance ID must be empty or a canonical GUID string.", nameof(instanceId));
            }

            InstanceId = instanceId;
            Metadata = metadata?.Clone() ?? ItemInstanceMetadata.WithoutInstanceState();
        }

        public IInventoryItemDefinition Definition { get; }
        public string DefinitionId => Definition.Id;
        public string InstanceId { get; private set; }
        public bool HasPersistentIdentity => !string.IsNullOrWhiteSpace(InstanceId);
        public ItemInstanceMetadata Metadata { get; private set; }
        public bool HasMetadata => Metadata != null && Metadata.HasAnyState;
        public bool RequiresPersistentIdentity => GetRequiresPersistentIdentity(Definition, Metadata);

        public static ItemInstance CreateDefinitionOnly(IInventoryItemDefinition definition)
        {
            return new ItemInstance(definition);
        }

        public static ItemInstance CreateStateful(IInventoryItemDefinition definition, ItemInstanceMetadata metadata, string instanceId = null)
        {
            return new ItemInstance(definition, string.IsNullOrWhiteSpace(instanceId) ? ItemInstanceId.Generate() : instanceId, metadata);
        }

        public ItemInstance CloneWithNewIdentity()
        {
            return new ItemInstance(Definition, ItemInstanceId.Generate(), Metadata);
        }

        public ItemInstance CloneDefinitionOnly()
        {
            return new ItemInstance(Definition, null, Metadata);
        }

        public void AssignMetadata(ItemInstanceMetadata metadata)
        {
            Metadata = metadata?.Clone() ?? ItemInstanceMetadata.WithoutInstanceState();
        }

        public void SetCondition(float conditionNormalized)
        {
            Metadata = Metadata == null
                ? ItemInstanceMetadata.WithCondition(conditionNormalized)
                : Metadata.WithConditionValue(conditionNormalized);
        }

        public void ClearCondition()
        {
            Metadata = Metadata == null
                ? ItemInstanceMetadata.WithoutInstanceState()
                : Metadata.WithoutCondition();
        }

        public void SetQuality(QualityDefinition quality)
        {
            Metadata = Metadata == null
                ? ItemInstanceMetadata.WithQuality(quality)
                : Metadata.WithQualityValue(quality);
        }

        public void ClearQuality()
        {
            Metadata = Metadata == null
                ? ItemInstanceMetadata.WithoutInstanceState()
                : Metadata.WithQualityValue(null);
        }

        public void EnsurePersistentIdentity()
        {
            if (string.IsNullOrWhiteSpace(InstanceId))
            {
                InstanceId = ItemInstanceId.Generate();
            }
        }

        private static bool GetRequiresPersistentIdentity(IInventoryItemDefinition definition, ItemInstanceMetadata metadata)
        {
            if (definition is IItemInstancePolicy { InstanceMode: ItemInstanceMode.AlwaysInstanced })
            {
                return true;
            }

            return metadata != null && metadata.HasAnyState;
        }
    }
}
