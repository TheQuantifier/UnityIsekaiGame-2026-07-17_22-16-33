using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Abilities;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Stats;

namespace UnityIsekaiGame.StatusEffects
{
    [CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Unity Isekai Game/Status Effects/Status Effect")]
    public sealed class StatusEffectDefinition : ScriptableObject, IGameDefinition, ICategorizableDefinition, ITaggedDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string statusId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private CategoryDefinition primaryCategory;
        [SerializeField] private TagDefinition[] tags;
        [SerializeField] private StatusEffectDisposition disposition;
        [SerializeField] private StatusDurationModel durationModel = StatusDurationModel.Timed;
        [SerializeField, Min(0f)] private float defaultDuration = 8f;
        [SerializeField] private StatusStackingPolicy stackingPolicy = StatusStackingPolicy.RefreshDuration;
        [SerializeField] private StatusRefreshPolicy refreshPolicy = StatusRefreshPolicy.ResetToFullDuration;
        [SerializeField, Min(1)] private int maximumStacks = 1;
        [SerializeField] private bool canBeRemoved = true;
        [SerializeField] private bool visibleInHud = true;
        [SerializeField] private StatModifierDefinition[] statModifiers;
        [SerializeField, Min(0f)] private float periodicInterval;
        [SerializeField] private EffectDefinition[] periodicEffects;

        public string StatusId => statusId;
        public string Id => statusId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public CategoryDefinition PrimaryCategory => primaryCategory;
        public CategoryDomain ClassificationDomain => CategoryDomain.General;
        public IReadOnlyList<TagDefinition> Tags => tags ?? System.Array.Empty<TagDefinition>();
        public StatusEffectDisposition Disposition => disposition;
        public StatusDurationModel DurationModel => durationModel;
        public float DefaultDuration => defaultDuration;
        public StatusStackingPolicy StackingPolicy => stackingPolicy;
        public StatusRefreshPolicy RefreshPolicy => refreshPolicy;
        public int MaximumStacks => Mathf.Max(1, maximumStacks);
        public bool CanBeRemoved => canBeRemoved;
        public bool VisibleInHud => visibleInHud;
        public IReadOnlyList<StatModifierDefinition> StatModifiers => statModifiers ?? System.Array.Empty<StatModifierDefinition>();
        public float PeriodicInterval => periodicInterval;
        public IReadOnlyList<EffectDefinition> PeriodicEffects => periodicEffects ?? System.Array.Empty<EffectDefinition>();

        private void OnValidate()
        {
            defaultDuration = Mathf.Max(0f, defaultDuration);
            maximumStacks = Mathf.Max(1, maximumStacks);
            periodicInterval = Mathf.Max(0f, periodicInterval);
        }

        public float ResolveDuration(float overrideDuration)
        {
            return overrideDuration > 0f ? overrideDuration : defaultDuration;
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (primaryCategory == null)
            {
                report.AddError($"Status effect '{DisplayName}' has no category.");
            }

            if (durationModel == StatusDurationModel.Timed && defaultDuration <= 0f)
            {
                report.AddError($"Timed status effect '{DisplayName}' must have a positive default duration.");
            }

            if (durationModel == StatusDurationModel.Instant && (statModifiers?.Length ?? 0) > 0)
            {
                report.AddWarning($"Instant status effect '{DisplayName}' has stat modifiers that will not remain active.");
            }

            if (maximumStacks < 1)
            {
                report.AddError($"Status effect '{DisplayName}' must allow at least one stack.");
            }

            if (stackingPolicy == StatusStackingPolicy.AddStack && maximumStacks < 2)
            {
                report.AddError($"Status effect '{DisplayName}' uses AddStack but maximum stacks is below two.");
            }

            if (stackingPolicy != StatusStackingPolicy.AddStack && maximumStacks > 1)
            {
                report.AddWarning($"Status effect '{DisplayName}' has maximum stacks above one but does not use AddStack.");
            }

            ValidateModifierDefinitions(report);
            ValidatePeriodicConfiguration(report);
        }

        private void ValidateModifierDefinitions(DefinitionValidationReport report)
        {
            if (statModifiers == null)
            {
                return;
            }

            HashSet<string> seenModifiers = new HashSet<string>();
            for (int i = 0; i < statModifiers.Length; i++)
            {
                StatModifierDefinition modifier = statModifiers[i];
                if (modifier == null)
                {
                    report.AddError($"Status effect '{DisplayName}' has a null stat modifier at index {i}.");
                    continue;
                }

                if (!modifier.IsValid)
                {
                    report.AddError($"Status effect '{DisplayName}' has an invalid modifier value at index {i}.");
                }

                string key = $"{modifier.StatType}:{modifier.Operation}:{modifier.Value}:{modifier.Priority}";
                if (!seenModifiers.Add(key))
                {
                    report.AddWarning($"Status effect '{DisplayName}' has duplicate-looking stat modifier '{key}'.");
                }
            }
        }

        private void ValidatePeriodicConfiguration(DefinitionValidationReport report)
        {
            bool hasPeriodicEffects = periodicEffects != null && periodicEffects.Length > 0;
            if (periodicInterval > 0f && !hasPeriodicEffects)
            {
                report.AddWarning($"Status effect '{DisplayName}' has a periodic interval but no periodic effects.");
            }

            if (hasPeriodicEffects && periodicInterval <= 0f)
            {
                report.AddError($"Status effect '{DisplayName}' has periodic effects but no positive interval.");
            }

            if (periodicEffects == null)
            {
                return;
            }

            for (int i = 0; i < periodicEffects.Length; i++)
            {
                if (periodicEffects[i] == null)
                {
                    report.AddError($"Status effect '{DisplayName}' has a null periodic effect at index {i}.");
                }
            }
        }
    }
}
