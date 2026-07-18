using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityIsekaiGame.GameData
{
    [CreateAssetMenu(fileName = "DefinitionCatalog", menuName = "Unity Isekai Game/Game Data/Definition Catalog")]
    public sealed class DefinitionCatalog : ScriptableObject
    {
        [SerializeField] private string catalogId = "catalog.prototype";
        [SerializeField] private string contentVersion = "0.3.0-step-3";
        [SerializeField] private ScriptableObject[] definitions;

        public string CatalogId => catalogId;
        public string ContentVersion => contentVersion;
        public IReadOnlyList<ScriptableObject> DefinitionAssets => definitions ?? Array.Empty<ScriptableObject>();

        public IEnumerable<IGameDefinition> GetDefinitions()
        {
            IReadOnlyList<ScriptableObject> assets = DefinitionAssets;
            for (int i = 0; i < assets.Count; i++)
            {
                if (assets[i] is IGameDefinition definition)
                {
                    yield return definition;
                }
            }
        }

        public DefinitionRegistry CreateRegistry(DefinitionValidationReport report = null)
        {
            return new DefinitionRegistry(GetDefinitions(), report);
        }
    }
}
