using System.Collections.Generic;

namespace UnityIsekaiGame.GameData
{
    public static class ItemInstanceValidationUtility
    {
        public static DefinitionValidationReport ValidateUniqueInstanceIds(IEnumerable<ItemInstance> itemInstances)
        {
            DefinitionValidationReport report = new DefinitionValidationReport();
            HashSet<string> seenIds = new HashSet<string>();

            if (itemInstances == null)
            {
                return report;
            }

            foreach (ItemInstance itemInstance in itemInstances)
            {
                if (itemInstance == null || string.IsNullOrWhiteSpace(itemInstance.InstanceId))
                {
                    continue;
                }

                if (!ItemInstanceId.IsValid(itemInstance.InstanceId))
                {
                    report.AddError($"Item instance ID '{itemInstance.InstanceId}' is malformed.");
                    continue;
                }

                if (!seenIds.Add(itemInstance.InstanceId))
                {
                    report.AddError($"Duplicate item instance ID '{itemInstance.InstanceId}' found in supplied runtime item collection.");
                }
            }

            return report;
        }
    }
}
