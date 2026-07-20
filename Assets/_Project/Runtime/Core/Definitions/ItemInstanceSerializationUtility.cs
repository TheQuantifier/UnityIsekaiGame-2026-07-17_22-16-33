using UnityEngine;

namespace UnityIsekaiGame.GameData
{
    public static class ItemInstanceSerializationUtility
    {
        public static ItemInstanceSaveData CreateSaveData(ItemInstance itemInstance)
        {
            return TryCreateSaveData(itemInstance).SaveData;
        }

        public static ItemInstanceSerializationResult TryCreateSaveData(ItemInstance itemInstance)
        {
            if (itemInstance == null || itemInstance.Definition == null)
            {
                return ItemInstanceSerializationResult.Failure("Cannot serialize a missing item instance or item definition.");
            }

            ItemInstanceMetadata metadata = itemInstance.Metadata;
            ItemInstanceSaveData saveData = new ItemInstanceSaveData
            {
                definitionId = itemInstance.Definition.Id,
                instanceId = itemInstance.InstanceId,
                hasQuality = metadata != null && metadata.HasQuality,
                qualityId = metadata != null && metadata.Quality != null ? metadata.Quality.Id : null,
                hasCondition = metadata != null && metadata.HasCondition,
                conditionNormalized = metadata != null && metadata.HasCondition ? metadata.ConditionNormalized : 0f
            };

            return ItemInstanceSerializationResult.Success(saveData);
        }

        public static ItemInstanceRestoreResult Restore(ItemInstanceSaveData saveData, DefinitionRegistry registry)
        {
            if (saveData == null)
            {
                return ItemInstanceRestoreResult.Failure(ItemInstanceRestoreStatus.MissingSaveData, "Item instance save data is missing.");
            }

            if (string.IsNullOrWhiteSpace(saveData.definitionId))
            {
                return ItemInstanceRestoreResult.Failure(ItemInstanceRestoreStatus.MissingDefinitionId, "Item instance save data has no definition ID.");
            }

            if (!ItemInstanceId.IsValid(saveData.instanceId))
            {
                return ItemInstanceRestoreResult.Failure(ItemInstanceRestoreStatus.InvalidInstanceId, $"Item instance ID '{saveData.instanceId}' is not a canonical GUID string.");
            }

            if (registry == null || !registry.TryGet(saveData.definitionId, out IGameDefinition definition))
            {
                return ItemInstanceRestoreResult.Failure(ItemInstanceRestoreStatus.MissingItemDefinition, $"Item definition '{saveData.definitionId}' was not found.");
            }

            if (definition is not IInventoryItemDefinition itemDefinition)
            {
                return ItemInstanceRestoreResult.Failure(ItemInstanceRestoreStatus.WrongDefinitionType, $"Definition '{saveData.definitionId}' is not an inventory item definition.");
            }

            if (saveData.hasCondition && (float.IsNaN(saveData.conditionNormalized) || float.IsInfinity(saveData.conditionNormalized) || saveData.conditionNormalized < 0f || saveData.conditionNormalized > 1f))
            {
                return ItemInstanceRestoreResult.Failure(ItemInstanceRestoreStatus.InvalidConditionValue, $"Condition value {saveData.conditionNormalized} is outside 0..1.");
            }

            QualityDefinition quality = null;
            if (saveData.hasQuality)
            {
                if (string.IsNullOrWhiteSpace(saveData.qualityId)
                    || !registry.TryGet(saveData.qualityId, out quality))
                {
                    return ItemInstanceRestoreResult.Failure(ItemInstanceRestoreStatus.MissingQualityDefinition, $"Quality definition '{saveData.qualityId}' was not found.");
                }
            }

            ItemInstanceMetadata metadata = saveData.hasQuality || saveData.hasCondition
                ? new ItemInstanceMetadata(quality, saveData.hasCondition ? Mathf.Clamp01(saveData.conditionNormalized) : null)
                : ItemInstanceMetadata.WithoutInstanceState();
            ItemInstance itemInstance = new ItemInstance(itemDefinition, saveData.instanceId, metadata);

            return ItemInstanceRestoreResult.Success(itemInstance);
        }
    }
}
