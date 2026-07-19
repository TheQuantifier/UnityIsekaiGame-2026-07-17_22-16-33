using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Combat;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Beings
{
    [CreateAssetMenu(fileName = "ActorProfile", menuName = "Unity Isekai Game/Beings/Actor Profile")]
    public sealed class ActorProfileDefinition : ScriptableObject, IGameDefinition, ICategorizableDefinition, ITaggedDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string actorProfileId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private BeingDefinition beingDefinition;
        [SerializeField] private CategoryDefinition primaryCategory;
        [SerializeField] private TagDefinition[] tags;
        [SerializeField, Min(1f)] private float baseMaximumHealth = 100f;
        [SerializeField, Min(0f)] private float baseMaximumStamina = 100f;
        [SerializeField, Min(0f)] private float baseMaximumMana = 100f;
        [SerializeField, Min(0f)] private float baseAttackPower;
        [SerializeField, Min(0f)] private float baseDefense;
        [SerializeField, Min(0f)] private float baseMovementSpeed;
        [SerializeField] private ResistanceModifierDefinition[] baseResistances;
        [SerializeField] private string futureSensesPlaceholder;
        [SerializeField] private string futureMovementProfilePlaceholder;

        public string ActorProfileId => actorProfileId;
        public string Id => actorProfileId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public BeingDefinition BeingDefinition => beingDefinition;
        public CategoryDefinition PrimaryCategory => primaryCategory;
        public CategoryDomain ClassificationDomain => CategoryDomain.Being;
        public IReadOnlyList<TagDefinition> Tags => tags ?? Array.Empty<TagDefinition>();
        public float BaseMaximumHealth => baseMaximumHealth;
        public float BaseMaximumStamina => baseMaximumStamina;
        public float BaseMaximumMana => baseMaximumMana;
        public float BaseAttackPower => baseAttackPower;
        public float BaseDefense => baseDefense;
        public float BaseMovementSpeed => baseMovementSpeed;
        public IReadOnlyList<ResistanceModifierDefinition> BaseResistances => baseResistances ?? System.Array.Empty<ResistanceModifierDefinition>();
        public string FutureSensesPlaceholder => futureSensesPlaceholder;
        public string FutureMovementProfilePlaceholder => futureMovementProfilePlaceholder;
        public bool HasValidBaseStats => IsFinite(baseMaximumHealth)
            && IsFinite(baseMaximumStamina)
            && IsFinite(baseMaximumMana)
            && IsFinite(baseAttackPower)
            && IsFinite(baseDefense)
            && IsFinite(baseMovementSpeed)
            && baseMaximumHealth >= 1f
            && baseMaximumStamina >= 0f
            && baseMaximumMana >= 0f
            && baseAttackPower >= 0f
            && baseDefense >= 0f
            && baseMovementSpeed >= 0f;

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (definitionsById == null || report == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(Id) && !Id.StartsWith("actor-profile."))
            {
                report.AddWarning($"ActorProfileDefinition '{DisplayName}' should use the 'actor-profile.' namespace prefix.");
            }

            if (beingDefinition == null)
            {
                report.AddError($"ActorProfileDefinition '{DisplayName}' is missing a BeingDefinition reference.");
            }
            else if (!definitionsById.TryGetValue(beingDefinition.Id, out IGameDefinition being) || !(being is BeingDefinition))
            {
                report.AddError($"ActorProfileDefinition '{DisplayName}' references being '{beingDefinition.Id}', which is not in the configured catalog.");
            }

            ValidateBaseStat(baseMaximumHealth, 1f, "base maximum health", report);
            ValidateBaseStat(baseMaximumStamina, 0f, "base maximum stamina", report);
            ValidateBaseStat(baseMaximumMana, 0f, "base maximum mana", report);
            ValidateBaseStat(baseAttackPower, 0f, "base attack power", report);
            ValidateBaseStat(baseDefense, 0f, "base defense", report);
            ValidateBaseStat(baseMovementSpeed, 0f, "base movement speed", report);
            ValidateBaseResistances(definitionsById, report);

            if (primaryCategory == null && beingDefinition != null && beingDefinition.PrimaryCategory == null)
            {
                report.AddWarning($"ActorProfileDefinition '{DisplayName}' has no profile category and its being has no category.");
            }
        }

        private void OnValidate()
        {
            baseMaximumHealth = Mathf.Max(1f, baseMaximumHealth);
            baseMaximumStamina = Mathf.Max(0f, baseMaximumStamina);
            baseMaximumMana = Mathf.Max(0f, baseMaximumMana);
            baseAttackPower = Mathf.Max(0f, baseAttackPower);
            baseDefense = Mathf.Max(0f, baseDefense);
            baseMovementSpeed = Mathf.Max(0f, baseMovementSpeed);
        }

        private void ValidateBaseStat(float value, float minimum, string label, DefinitionValidationReport report)
        {
            if (!IsFinite(value))
            {
                report.AddError($"ActorProfileDefinition '{DisplayName}' has non-finite {label}.");
            }
            else if (value < minimum)
            {
                report.AddError($"ActorProfileDefinition '{DisplayName}' has {label} below {minimum:0.#}.");
            }
        }

        private void ValidateBaseResistances(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (baseResistances == null)
            {
                return;
            }

            HashSet<string> seenDamageTypes = new HashSet<string>();
            for (int i = 0; i < baseResistances.Length; i++)
            {
                ResistanceModifierDefinition resistance = baseResistances[i];
                if (resistance == null)
                {
                    report.AddError($"ActorProfileDefinition '{DisplayName}' has a null base resistance at index {i}.");
                    continue;
                }

                if (resistance.DamageType == null)
                {
                    report.AddError($"ActorProfileDefinition '{DisplayName}' has a base resistance with no damage type at index {i}.");
                    continue;
                }

                if (!RuntimeResistanceCollection.IsSupportedResistance(resistance.Resistance))
                {
                    report.AddError($"ActorProfileDefinition '{DisplayName}' has resistance {resistance.Resistance:0.###} for '{resistance.DamageType.Id}', outside -1 to 1.");
                }

                if (!seenDamageTypes.Add(resistance.DamageType.Id))
                {
                    report.AddError($"ActorProfileDefinition '{DisplayName}' has duplicate base resistance for '{resistance.DamageType.Id}'.");
                }

                if (definitionsById == null
                    || !definitionsById.TryGetValue(resistance.DamageType.Id, out IGameDefinition found)
                    || found is not DamageTypeDefinition)
                {
                    report.AddError($"ActorProfileDefinition '{DisplayName}' references damage type '{resistance.DamageType.Id}', which is not in the configured catalog.");
                }
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
