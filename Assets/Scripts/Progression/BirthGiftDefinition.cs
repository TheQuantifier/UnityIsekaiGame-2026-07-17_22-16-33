using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Progression
{
    [CreateAssetMenu(fileName = "BirthGiftDefinition", menuName = "Unity Isekai Game/Progression/Birth Gift Definition")]
    public sealed class BirthGiftDefinition : ScriptableObject, IGameDefinition, ICategorizableDefinition, ITaggedDefinition, IHasRarity, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string birthGiftId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private CategoryDefinition primaryCategory;
        [SerializeField] private TagDefinition[] tags;
        [SerializeField] private RarityDefinition rarity;
        [SerializeField, Min(0f)] private float selectionWeight = 1f;
        [SerializeField] private BirthGiftType giftType = BirthGiftType.PermanentStatGrant;
        [SerializeField] private ProgressionAbilityReference grantedAbility;
        [SerializeField] private PermanentStatGrantDefinition[] permanentStatGrants;
        [SerializeField] private PermanentStatGrantDefinition[] futureGrowthAffinityEntries;
        [SerializeField] private BirthGiftAwakeningMode awakeningMode = BirthGiftAwakeningMode.ImmediateAutomatic;
        [SerializeField, Min(0f)] private float requiredActivePlaytimeSeconds;
        [SerializeField] private bool enabledForAlpha = true;
        [SerializeField] private string futureConditionData;

        public string BirthGiftId => birthGiftId;
        public string Id => birthGiftId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public CategoryDefinition PrimaryCategory => primaryCategory;
        public CategoryDomain ClassificationDomain => CategoryDomain.BirthGift;
        public IReadOnlyList<TagDefinition> Tags => tags ?? System.Array.Empty<TagDefinition>();
        public RarityDefinition Rarity => rarity;
        public float SelectionWeight => Mathf.Max(0f, selectionWeight);
        public BirthGiftType GiftType => giftType;
        public ProgressionAbilityReference GrantedAbility => grantedAbility;
        public IReadOnlyList<PermanentStatGrantDefinition> PermanentStatGrants => permanentStatGrants ?? System.Array.Empty<PermanentStatGrantDefinition>();
        public IReadOnlyList<PermanentStatGrantDefinition> FutureGrowthAffinityEntries => futureGrowthAffinityEntries ?? System.Array.Empty<PermanentStatGrantDefinition>();
        public BirthGiftAwakeningMode AwakeningMode => awakeningMode;
        public float RequiredActivePlaytimeSeconds => Mathf.Max(0f, requiredActivePlaytimeSeconds);
        public bool EnabledForAlpha => enabledForAlpha;
        public string FutureConditionData => futureConditionData ?? string.Empty;

        private void OnValidate()
        {
            selectionWeight = Mathf.Max(0f, selectionWeight);
            requiredActivePlaytimeSeconds = Mathf.Max(0f, requiredActivePlaytimeSeconds);
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (!Id.StartsWith("birth-gift."))
            {
                report.AddWarning($"Birth gift '{DisplayName}' should use the 'birth-gift.' namespace prefix.");
            }

            if (rarity == null)
            {
                report.AddError($"Birth gift '{DisplayName}' is missing a rarity.");
            }

            if (enabledForAlpha && selectionWeight <= 0f)
            {
                report.AddError($"Birth gift '{DisplayName}' is alpha-enabled but has no selection weight.");
            }

            if (!System.Enum.IsDefined(typeof(BirthGiftType), giftType))
            {
                report.AddError($"Birth gift '{DisplayName}' has an invalid gift type.");
            }

            if (!System.Enum.IsDefined(typeof(BirthGiftAwakeningMode), awakeningMode))
            {
                report.AddError($"Birth gift '{DisplayName}' has an invalid awakening mode.");
            }

            if (awakeningMode == BirthGiftAwakeningMode.DelayedActivePlaytime && requiredActivePlaytimeSeconds <= 0f)
            {
                report.AddError($"Birth gift '{DisplayName}' uses delayed active playtime without a delay.");
            }

            if (giftType == BirthGiftType.PermanentStatGrant && PermanentStatGrants.Count == 0)
            {
                report.AddError($"Birth gift '{DisplayName}' is a permanent stat grant but has no grants.");
            }

            if (giftType == BirthGiftType.LatentSkill && grantedAbility != null && !string.IsNullOrWhiteSpace(grantedAbility.AbilityId))
            {
                if (grantedAbility.Ability != null
                    && (definitionsById == null || !definitionsById.TryGetValue(grantedAbility.Ability.Id, out IGameDefinition found) || found is not UnityIsekaiGame.Abilities.AbilityDefinition))
                {
                    report.AddError($"Birth gift '{DisplayName}' references ability '{grantedAbility.Ability.Id}', which is not in the configured catalog.");
                }
            }

            foreach (PermanentStatGrantDefinition grant in PermanentStatGrants)
            {
                if (grant == null || !grant.IsValid)
                {
                    report.AddError($"Birth gift '{DisplayName}' has an invalid permanent stat grant.");
                }
            }
        }
    }
}
