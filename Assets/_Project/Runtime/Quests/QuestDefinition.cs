using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Factions;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.People;

namespace UnityIsekaiGame.Quests
{
    [CreateAssetMenu(fileName = "Quest", menuName = "Unity Isekai Game/Quests/Quest")]
    public sealed class QuestDefinition : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string questId;
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 4)] private string summary;
        [SerializeField, TextArea(3, 8)] private string detailedDescription;
        [SerializeField] private QuestCategory category = QuestCategory.SideQuest;
        [SerializeField] private PersonDefinition questGiver;
        [SerializeField] private FactionDefinition questSourceFaction;
        [SerializeField] private FactionDefinition relatedFaction;
        [Tooltip("Legacy fallback used only when Quest Giver is not assigned.")]
        [SerializeField] private string questGiverId;
        [Tooltip("Legacy fallback used only when Quest Giver is not assigned.")]
        [SerializeField] private string questGiverDisplayName;
        [SerializeField] private QuestStageDefinition[] stages;
        [SerializeField] private ContractRewardDefinition reward;
        [SerializeField] private string[] prerequisiteQuestIds;
        [Header("Identity Metadata")]
        [SerializeField] private QuestDefinitionImportance importance = QuestDefinitionImportance.Standard;
        [SerializeField] private QuestDefinitionRepeatabilityPolicy repeatabilityPolicy = QuestDefinitionRepeatabilityPolicy.Unique;
        [SerializeField] private QuestVisibility defaultVisibility = QuestVisibility.Public;
        [SerializeField] private QuestSourceChannel defaultSourceChannel = QuestSourceChannel.Manual;
        [SerializeField] private QuestIssuerType[] supportedIssuerTypes = Array.Empty<QuestIssuerType>();
        [SerializeField] private QuestRecipientScope[] supportedRecipientScopes = Array.Empty<QuestRecipientScope>();
        [SerializeField] private string[] defaultTagIds = Array.Empty<string>();
        [SerializeField] private string[] supportedSubjectRoleIds = Array.Empty<string>();
        [SerializeField] private bool allowDynamicInstances;
        [SerializeField] private bool allowMultipleSimultaneousInstances;
        [SerializeField] private bool uniquePerWorld = true;
        [SerializeField] private bool uniquePerRecipient;
        [SerializeField] private string identityNotes;
        [Header("Participation Policy")]
        [SerializeField] private QuestAssignmentPolicy assignmentPolicy = QuestAssignmentPolicy.Exclusive;
        [SerializeField] private QuestConsentPolicy consentPolicy = QuestConsentPolicy.ExplicitRecipientConsentRequired;
        [SerializeField] private QuestRefusalPolicy refusalPolicy = QuestRefusalPolicy.MayReoffer;
        [SerializeField] private QuestAbandonmentPolicy abandonmentPolicy = QuestAbandonmentPolicy.AllowedReleasesCapacity;
        [SerializeField] private int assignmentCapacity = 1;
        [SerializeField] private double availabilityStartWorldTime = -1d;
        [SerializeField] private double availabilityEndWorldTime = -1d;
        [SerializeField] private double defaultOfferDuration = -1d;
        [SerializeField] private bool issuerWithdrawalAllowed = true;
        [SerializeField] private bool prevalidateEligibilityForOffers = true;
        [SerializeField] private string[] offeringAuthorityRequirementIds = Array.Empty<string>();
        [SerializeField] private QuestEligibilityRequirementGroupData[] eligibilityRequirementGroups = Array.Empty<QuestEligibilityRequirementGroupData>();
        [SerializeField] private bool repeatable;
        [SerializeField] private bool hiddenUntilDiscovered;
        [SerializeField] private bool canAbandon = true;

        public string QuestId => questId;
        public string Id => questId;
        public string DisplayName => Title;
        public string Title => string.IsNullOrWhiteSpace(title) ? "Untitled Quest" : title;
        public string Summary => summary;
        public string DetailedDescription => detailedDescription;
        public QuestCategory Category => category;
        public PersonDefinition QuestGiver => questGiver;
        public FactionDefinition QuestSourceFaction => questSourceFaction;
        public FactionDefinition RelatedFaction => relatedFaction;
        public string QuestSourceDisplayName => questSourceFaction == null ? QuestGiverDisplayName : questSourceFaction.DisplayName;
        public string QuestGiverId => questGiver == null ? questGiverId : questGiver.PersonId;
        public string QuestGiverDisplayName => questGiver == null
            ? questGiverDisplayName
            : string.IsNullOrWhiteSpace(questGiver.Title)
                ? questGiver.DisplayName
                : $"{questGiver.DisplayName}, {questGiver.Title}";
        public IReadOnlyList<QuestStageDefinition> Stages => stages ?? Array.Empty<QuestStageDefinition>();
        public ContractRewardDefinition Reward => reward;
        public IReadOnlyList<string> PrerequisiteQuestIds => prerequisiteQuestIds ?? Array.Empty<string>();
        public QuestDefinitionImportance Importance => importance;
        public QuestDefinitionRepeatabilityPolicy RepeatabilityPolicy => repeatabilityPolicy == QuestDefinitionRepeatabilityPolicy.Unknown && repeatable ? QuestDefinitionRepeatabilityPolicy.Reusable : repeatabilityPolicy;
        public QuestVisibility DefaultVisibility => hiddenUntilDiscovered && defaultVisibility == QuestVisibility.Public ? QuestVisibility.Hidden : defaultVisibility;
        public QuestSourceChannel DefaultSourceChannel => defaultSourceChannel;
        public IReadOnlyList<QuestIssuerType> SupportedIssuerTypes => supportedIssuerTypes ?? Array.Empty<QuestIssuerType>();
        public IReadOnlyList<QuestRecipientScope> SupportedRecipientScopes => supportedRecipientScopes ?? Array.Empty<QuestRecipientScope>();
        public IReadOnlyList<string> DefaultTagIds => defaultTagIds ?? Array.Empty<string>();
        public IReadOnlyList<string> SupportedSubjectRoleIds => supportedSubjectRoleIds ?? Array.Empty<string>();
        public bool AllowDynamicInstances => allowDynamicInstances || RepeatabilityPolicy == QuestDefinitionRepeatabilityPolicy.DynamicTemplate;
        public bool AllowMultipleSimultaneousInstances => allowMultipleSimultaneousInstances || repeatable || RepeatabilityPolicy == QuestDefinitionRepeatabilityPolicy.Reusable || RepeatabilityPolicy == QuestDefinitionRepeatabilityPolicy.DynamicTemplate;
        public bool UniquePerWorld => uniquePerWorld && !AllowMultipleSimultaneousInstances;
        public bool UniquePerRecipient => uniquePerRecipient;
        public string IdentityNotes => identityNotes ?? string.Empty;
        public QuestAssignmentPolicy AssignmentPolicy => assignmentPolicy == QuestAssignmentPolicy.Unknown ? QuestAssignmentPolicy.Exclusive : assignmentPolicy;
        public QuestConsentPolicy ConsentPolicy => consentPolicy == QuestConsentPolicy.Unknown ? QuestConsentPolicy.ExplicitRecipientConsentRequired : consentPolicy;
        public QuestRefusalPolicy RefusalPolicy => refusalPolicy == QuestRefusalPolicy.Unknown ? QuestRefusalPolicy.MayReoffer : refusalPolicy;
        public QuestAbandonmentPolicy AbandonmentPolicy => abandonmentPolicy == QuestAbandonmentPolicy.Unknown ? canAbandon ? QuestAbandonmentPolicy.AllowedReleasesCapacity : QuestAbandonmentPolicy.NotAllowed : abandonmentPolicy;
        public int AssignmentCapacity => AssignmentPolicy == QuestAssignmentPolicy.Nonexclusive ? Math.Max(assignmentCapacity, 0) : Math.Max(1, assignmentCapacity);
        public double AvailabilityStartWorldTime => availabilityStartWorldTime;
        public double AvailabilityEndWorldTime => availabilityEndWorldTime;
        public double DefaultOfferDuration => defaultOfferDuration;
        public bool IssuerWithdrawalAllowed => issuerWithdrawalAllowed;
        public bool PrevalidateEligibilityForOffers => prevalidateEligibilityForOffers;
        public IReadOnlyList<string> OfferingAuthorityRequirementIds => offeringAuthorityRequirementIds ?? Array.Empty<string>();
        public IReadOnlyList<QuestEligibilityRequirementGroupData> EligibilityRequirementGroups => (eligibilityRequirementGroups ?? Array.Empty<QuestEligibilityRequirementGroupData>()).Where(value => value != null).Select(value => value.Clone()).ToArray();
        public bool Repeatable => repeatable;
        public bool HiddenUntilDiscovered => hiddenUntilDiscovered;
        public bool CanAbandon => canAbandon;

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (definitionsById == null || report == null)
            {
                return;
            }

            ValidateStageAndObjectiveIds(report);
            ValidateIdentityMetadata(report);
            ValidatePersonReference(questGiver, nameof(QuestGiver), definitionsById, report);
            ValidateFactionReference(questSourceFaction, nameof(QuestSourceFaction), definitionsById, report);
            ValidateFactionReference(relatedFaction, nameof(RelatedFaction), definitionsById, report);
        }

        public void DevelopmentConfigureIdentity(
            string id,
            string displayTitle,
            QuestCategory questCategory,
            QuestDefinitionImportance questImportance = QuestDefinitionImportance.Standard,
            QuestDefinitionRepeatabilityPolicy repeatPolicy = QuestDefinitionRepeatabilityPolicy.Unique,
            QuestVisibility visibility = QuestVisibility.Public,
            QuestSourceChannel sourceChannel = QuestSourceChannel.Manual,
            IEnumerable<QuestIssuerType> issuerTypes = null,
            IEnumerable<QuestRecipientScope> recipientScopes = null,
            IEnumerable<string> tags = null,
            bool dynamicInstances = false,
            bool multipleSimultaneousInstances = false,
            bool perWorldUnique = true,
            bool perRecipientUnique = false)
        {
            questId = id ?? string.Empty;
            title = string.IsNullOrWhiteSpace(displayTitle) ? id : displayTitle;
            category = questCategory;
            importance = questImportance;
            repeatabilityPolicy = repeatPolicy;
            defaultVisibility = visibility;
            defaultSourceChannel = sourceChannel;
            supportedIssuerTypes = DistinctEnums(issuerTypes);
            supportedRecipientScopes = DistinctEnums(recipientScopes);
            defaultTagIds = Clean(tags);
            allowDynamicInstances = dynamicInstances;
            allowMultipleSimultaneousInstances = multipleSimultaneousInstances;
            uniquePerWorld = perWorldUnique;
            uniquePerRecipient = perRecipientUnique;
            repeatable = repeatPolicy == QuestDefinitionRepeatabilityPolicy.Reusable || repeatPolicy == QuestDefinitionRepeatabilityPolicy.RepeatablePerIssuer || repeatPolicy == QuestDefinitionRepeatabilityPolicy.RepeatablePerRecipient || repeatPolicy == QuestDefinitionRepeatabilityPolicy.DynamicTemplate;
            hiddenUntilDiscovered = visibility == QuestVisibility.Hidden || visibility == QuestVisibility.Secret;
        }

        public void DevelopmentConfigureParticipation(
            QuestAssignmentPolicy assignment = QuestAssignmentPolicy.Exclusive,
            QuestConsentPolicy consent = QuestConsentPolicy.ExplicitRecipientConsentRequired,
            QuestRefusalPolicy refusal = QuestRefusalPolicy.MayReoffer,
            QuestAbandonmentPolicy abandonment = QuestAbandonmentPolicy.AllowedReleasesCapacity,
            int capacity = 1,
            double availableFrom = -1d,
            double availableUntil = -1d,
            double offerDuration = -1d,
            bool withdrawalAllowed = true,
            bool prevalidateOffers = true,
            IEnumerable<string> authorityRequirements = null,
            IEnumerable<QuestEligibilityRequirementGroupData> eligibilityGroups = null)
        {
            assignmentPolicy = assignment == QuestAssignmentPolicy.Unknown ? QuestAssignmentPolicy.Exclusive : assignment;
            consentPolicy = consent == QuestConsentPolicy.Unknown ? QuestConsentPolicy.ExplicitRecipientConsentRequired : consent;
            refusalPolicy = refusal == QuestRefusalPolicy.Unknown ? QuestRefusalPolicy.MayReoffer : refusal;
            abandonmentPolicy = abandonment == QuestAbandonmentPolicy.Unknown ? QuestAbandonmentPolicy.AllowedReleasesCapacity : abandonment;
            assignmentCapacity = Math.Max(assignmentPolicy == QuestAssignmentPolicy.Nonexclusive ? 0 : 1, capacity);
            availabilityStartWorldTime = availableFrom;
            availabilityEndWorldTime = availableUntil;
            defaultOfferDuration = offerDuration;
            issuerWithdrawalAllowed = withdrawalAllowed;
            prevalidateEligibilityForOffers = prevalidateOffers;
            offeringAuthorityRequirementIds = Clean(authorityRequirements);
            eligibilityRequirementGroups = (eligibilityGroups ?? Array.Empty<QuestEligibilityRequirementGroupData>()).Where(value => value != null).Select(value => value.Clone()).ToArray();
            canAbandon = AbandonmentPolicy != QuestAbandonmentPolicy.NotAllowed;
        }

        private void ValidateStageAndObjectiveIds(DefinitionValidationReport report)
        {
            HashSet<string> stageIds = new HashSet<string>();
            for (int stageIndex = 0; stageIndex < Stages.Count; stageIndex++)
            {
                QuestStageDefinition stage = Stages[stageIndex];
                if (stage == null)
                {
                    report.AddError($"QuestDefinition '{Title}' stage {stageIndex} is missing.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(stage.StageId))
                {
                    report.AddError($"QuestDefinition '{Title}' stage {stageIndex} is missing a stable stage ID.");
                }
                else if (!stageIds.Add(stage.StageId))
                {
                    report.AddError($"QuestDefinition '{Title}' has duplicate stage ID '{stage.StageId}'.");
                }

                HashSet<string> objectiveIds = new HashSet<string>();
                for (int objectiveIndex = 0; objectiveIndex < stage.Objectives.Count; objectiveIndex++)
                {
                    ContractObjectiveDefinition objective = stage.Objectives[objectiveIndex];
                    if (objective == null)
                    {
                        report.AddError($"QuestDefinition '{Title}' stage '{stage.StageId}' objective {objectiveIndex} is missing.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(objective.ObjectiveId))
                    {
                        report.AddError($"QuestDefinition '{Title}' stage '{stage.StageId}' objective {objectiveIndex} is missing a stable objective ID.");
                    }
                    else if (!objectiveIds.Add(objective.ObjectiveId))
                    {
                        report.AddError($"QuestDefinition '{Title}' stage '{stage.StageId}' has duplicate objective ID '{objective.ObjectiveId}'.");
                    }
                }
            }
        }

        private void ValidateIdentityMetadata(DefinitionValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                report.AddError($"QuestDefinition '{Title}' is missing a stable quest definition ID.");
            }

            if (category == QuestCategory.Unknown)
            {
                report.AddError($"QuestDefinition '{Title}' must declare a concrete quest category.");
            }

            if (RepeatabilityPolicy == QuestDefinitionRepeatabilityPolicy.Unknown)
            {
                report.AddError($"QuestDefinition '{Title}' must declare a concrete repeatability policy.");
            }

            if (DefaultVisibility == QuestVisibility.Unknown)
            {
                report.AddError($"QuestDefinition '{Title}' must declare a concrete default visibility.");
            }

            if (defaultSourceChannel == QuestSourceChannel.Unknown)
            {
                report.AddError($"QuestDefinition '{Title}' must declare a concrete source channel.");
            }

            if (AssignmentPolicy == QuestAssignmentPolicy.Unknown)
            {
                report.AddError($"QuestDefinition '{Title}' must declare a concrete assignment policy.");
            }

            if (ConsentPolicy == QuestConsentPolicy.Unknown)
            {
                report.AddError($"QuestDefinition '{Title}' must declare a concrete consent policy.");
            }

            if (AssignmentPolicy != QuestAssignmentPolicy.Nonexclusive && AssignmentCapacity <= 0)
            {
                report.AddError($"QuestDefinition '{Title}' assignment capacity must be positive for limited or exclusive assignment.");
            }

            if (AvailabilityEndWorldTime >= 0d && AvailabilityStartWorldTime >= 0d && AvailabilityEndWorldTime < AvailabilityStartWorldTime)
            {
                report.AddError($"QuestDefinition '{Title}' availability end cannot be before availability start.");
            }

            foreach (QuestEligibilityRequirementGroupData group in EligibilityRequirementGroups)
            {
                if (group.policy == QuestEligibilityGroupPolicy.Unknown)
                {
                    report.AddError($"QuestDefinition '{Title}' has an eligibility group with an unknown policy.");
                }

                foreach (QuestEligibilityRequirementData requirement in group.requirements ?? Array.Empty<QuestEligibilityRequirementData>())
                {
                    if (requirement.kind == QuestEligibilityRequirementKind.Unknown)
                    {
                        report.AddError($"QuestDefinition '{Title}' has an eligibility requirement with unknown kind.");
                    }
                }
            }
        }

        private void ValidatePersonReference(
            PersonDefinition person,
            string label,
            IReadOnlyDictionary<string, IGameDefinition> definitionsById,
            DefinitionValidationReport report)
        {
            if (person == null)
            {
                return;
            }

            if (!definitionsById.TryGetValue(person.Id, out IGameDefinition found) || found is not PersonDefinition)
            {
                report.AddError($"QuestDefinition '{Title}' references {label} '{person.Id}', which is not in the configured catalog.");
            }
        }

        private void ValidateFactionReference(
            FactionDefinition faction,
            string label,
            IReadOnlyDictionary<string, IGameDefinition> definitionsById,
            DefinitionValidationReport report)
        {
            if (faction == null)
            {
                return;
            }

            if (!definitionsById.TryGetValue(faction.Id, out IGameDefinition found) || found is not FactionDefinition)
            {
                report.AddError($"QuestDefinition '{Title}' references {label} '{faction.Id}', which is not in the configured catalog.");
            }
        }

        private static QuestIssuerType[] DistinctEnums(IEnumerable<QuestIssuerType> values)
        {
            return (values ?? Array.Empty<QuestIssuerType>())
                .Where(value => value != QuestIssuerType.Unknown)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
        }

        private static QuestRecipientScope[] DistinctEnums(IEnumerable<QuestRecipientScope> values)
        {
            return (values ?? Array.Empty<QuestRecipientScope>())
                .Where(value => value != QuestRecipientScope.Unknown)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
        }

        private static string[] Clean(IEnumerable<string> values)
        {
            return (values ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
