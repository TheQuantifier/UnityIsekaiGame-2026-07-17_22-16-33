using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Abilities
{
    [CreateAssetMenu(fileName = "NewAbilityDefinition", menuName = "Unity Isekai Game/Abilities/Ability Definition")]
    public sealed class AbilityDefinition : ScriptableObject, IGameDefinition, ICategorizableDefinition, ITaggedDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string abilityId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private CategoryDefinition primaryCategory;
        [SerializeField] private TagDefinition[] tags;
        [SerializeField, Min(0f)] private float activationTime;
        [SerializeField, Min(0f)] private float range;
        [SerializeField, Min(0f)] private float cooldownDuration;
        [SerializeField] private AbilityResourceCost[] resourceCosts;
        [SerializeField] private AbilityTargetingMode targetingMode = AbilityTargetingMode.Direction;
        [SerializeField] private AbilityDeliveryMode deliveryMode = AbilityDeliveryMode.Immediate;
        [SerializeField] private AbilityProjectileDelivery projectileDelivery;
        [SerializeField] private EffectDefinition[] effects;

        public string AbilityId => abilityId;
        public string Id => abilityId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public CategoryDefinition PrimaryCategory => primaryCategory;
        public CategoryDomain ClassificationDomain => CategoryDomain.Ability;
        public IReadOnlyList<TagDefinition> Tags => tags ?? System.Array.Empty<TagDefinition>();
        public float ActivationTime => activationTime;
        public float Range => range;
        public float CooldownDuration => cooldownDuration;
        public IReadOnlyList<AbilityResourceCost> ResourceCosts => resourceCosts ?? System.Array.Empty<AbilityResourceCost>();
        public AbilityTargetingMode TargetingMode => targetingMode;
        public AbilityDeliveryMode DeliveryMode => deliveryMode;
        public AbilityProjectileDelivery ProjectileDelivery => projectileDelivery;
        public IReadOnlyList<EffectDefinition> Effects => effects ?? System.Array.Empty<EffectDefinition>();

        private void OnValidate()
        {
            activationTime = Mathf.Max(0f, activationTime);
            range = Mathf.Max(0f, range);
            cooldownDuration = Mathf.Max(0f, cooldownDuration);
            projectileDelivery?.Validate();

            if (resourceCosts == null)
            {
                return;
            }

            for (int i = 0; i < resourceCosts.Length; i++)
            {
                resourceCosts[i].Validate();
            }
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            AbilityDefinitionValidator.ValidateAbility(this, definitionsById, report);
        }
    }
}
