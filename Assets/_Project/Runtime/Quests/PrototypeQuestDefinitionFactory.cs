using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Quests
{
    public static class PrototypeQuestDefinitionFactory
    {
        public const string GuildPostingDefinitionId = "quest-definition.prototype.guild-posting";
        public const string MerchantDeliveryDefinitionId = "quest-definition.prototype.merchant-delivery";
        public const string CivicInvestigationDefinitionId = "quest-definition.prototype.civic-investigation";
        public const string HiddenDungeonRumorDefinitionId = "quest-definition.prototype.hidden-dungeon-rumor";
        public const string DynamicBountyDefinitionId = "quest-definition.prototype.dynamic-bounty";

        public static readonly string[] PrototypeDefinitionIds =
        {
            GuildPostingDefinitionId,
            MerchantDeliveryDefinitionId,
            CivicInvestigationDefinitionId,
            HiddenDungeonRumorDefinitionId,
            DynamicBountyDefinitionId
        };

        public static DefinitionRegistry AddMissingPrototypeQuestDefinitions(DefinitionRegistry baseRegistry)
        {
            HashSet<string> ids = new HashSet<string>(baseRegistry?.DefinitionsById.Keys ?? Array.Empty<string>(), StringComparer.Ordinal);
            List<IGameDefinition> definitions = new List<IGameDefinition>();
            if (baseRegistry != null)
            {
                definitions.AddRange(baseRegistry.DefinitionsById.Values.Where(definition => definition != null));
            }

            foreach (QuestDefinition definition in CreateMissingQuestDefinitions(ids))
            {
                definitions.Add(definition);
            }

            return new DefinitionRegistry(definitions);
        }

        public static IReadOnlyList<QuestDefinition> CreateMissingQuestDefinitions(IEnumerable<string> existingDefinitionIds)
        {
            HashSet<string> ids = existingDefinitionIds == null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(existingDefinitionIds, StringComparer.Ordinal);
            List<QuestDefinition> definitions = new List<QuestDefinition>();
            Add(definitions, ids, GuildPostingDefinitionId, "Guild Posting", QuestCategory.GuildQuest, QuestDefinitionImportance.Standard, QuestDefinitionRepeatabilityPolicy.Unique, QuestVisibility.Public, QuestSourceChannel.QuestBoard, new[] { QuestIssuerType.Organization }, new[] { QuestRecipientScope.Open, QuestRecipientScope.Person }, new[] { "guild", "posting", "public" });
            Add(definitions, ids, MerchantDeliveryDefinitionId, "Merchant Delivery", QuestCategory.Delivery, QuestDefinitionImportance.Standard, QuestDefinitionRepeatabilityPolicy.RepeatablePerRecipient, QuestVisibility.LocallyKnown, QuestSourceChannel.Contract, new[] { QuestIssuerType.Organization, QuestIssuerType.Business }, new[] { QuestRecipientScope.Person }, new[] { "merchant", "delivery", "contract" }, multiple: true, perWorldUnique: false, perRecipientUnique: true);
            Add(definitions, ids, CivicInvestigationDefinitionId, "Civic Investigation", QuestCategory.Investigation, QuestDefinitionImportance.Important, QuestDefinitionRepeatabilityPolicy.Unique, QuestVisibility.Restricted, QuestSourceChannel.Government, new[] { QuestIssuerType.Government, QuestIssuerType.Office }, new[] { QuestRecipientScope.Person, QuestRecipientScope.Officeholder }, new[] { "civic", "investigation", "restricted" });
            Add(definitions, ids, HiddenDungeonRumorDefinitionId, "Hidden Dungeon Rumor", QuestCategory.Discovery, QuestDefinitionImportance.Minor, QuestDefinitionRepeatabilityPolicy.Unique, QuestVisibility.Hidden, QuestSourceChannel.Discovery, new[] { QuestIssuerType.Anonymous, QuestIssuerType.System }, new[] { QuestRecipientScope.Open }, new[] { "hidden", "dungeon", "rumor" });
            Add(definitions, ids, DynamicBountyDefinitionId, "Dynamic Bounty", QuestCategory.BountyPlaceholder, QuestDefinitionImportance.Standard, QuestDefinitionRepeatabilityPolicy.DynamicTemplate, QuestVisibility.Public, QuestSourceChannel.QuestBoard, new[] { QuestIssuerType.Organization, QuestIssuerType.Government }, new[] { QuestRecipientScope.Open, QuestRecipientScope.Person }, new[] { "bounty", "dynamic" }, dynamic: true, multiple: true, perWorldUnique: false);
            return definitions;
        }

        private static void Add(
            ICollection<QuestDefinition> definitions,
            ISet<string> existingIds,
            string id,
            string title,
            QuestCategory category,
            QuestDefinitionImportance importance,
            QuestDefinitionRepeatabilityPolicy repeatability,
            QuestVisibility visibility,
            QuestSourceChannel source,
            IEnumerable<QuestIssuerType> issuerTypes,
            IEnumerable<QuestRecipientScope> recipientScopes,
            IEnumerable<string> tags,
            bool dynamic = false,
            bool multiple = false,
            bool perWorldUnique = true,
            bool perRecipientUnique = false)
        {
            if (existingIds.Contains(id))
            {
                return;
            }

            QuestDefinition definition = ScriptableObject.CreateInstance<QuestDefinition>();
            definition.name = title;
            definition.DevelopmentConfigureIdentity(id, title, category, importance, repeatability, visibility, source, issuerTypes, recipientScopes, tags, dynamic, multiple, perWorldUnique, perRecipientUnique);
            ConfigureParticipation(definition, id);
            definitions.Add(definition);
            existingIds.Add(id);
        }

        private static void ConfigureParticipation(QuestDefinition definition, string id)
        {
            switch (id)
            {
                case GuildPostingDefinitionId:
                    definition.DevelopmentConfigureParticipation(
                        assignment: QuestAssignmentPolicy.Exclusive,
                        consent: QuestConsentPolicy.ExplicitRecipientConsentRequired,
                        refusal: QuestRefusalPolicy.MayReoffer,
                        abandonment: QuestAbandonmentPolicy.AllowedReleasesCapacity,
                        capacity: 1,
                        offerDuration: 7d,
                        authorityRequirements: new[] { "authority.prototype.guild.quest-offer" },
                        eligibilityGroups: new[] { All("guild-context", Membership("organization.prototype.adventurers-guild"), Interaction("interaction-point.prototype.guild-counter")) });
                    break;
                case MerchantDeliveryDefinitionId:
                    definition.DevelopmentConfigureParticipation(
                        assignment: QuestAssignmentPolicy.Nonexclusive,
                        consent: QuestConsentPolicy.ExplicitRecipientConsentRequired,
                        refusal: QuestRefusalPolicy.MayReoffer,
                        abandonment: QuestAbandonmentPolicy.AllowedReleasesCapacity,
                        capacity: 0,
                        offerDuration: 3d,
                        authorityRequirements: new[] { "authority.prototype.merchant.quest-offer" },
                        eligibilityGroups: new[] { All("merchant-counter", Interaction("interaction-point.prototype.merchant-counter")) });
                    break;
                case CivicInvestigationDefinitionId:
                    definition.DevelopmentConfigureParticipation(
                        assignment: QuestAssignmentPolicy.CapacityLimited,
                        consent: QuestConsentPolicy.DirectInstitutionalAssignmentAllowed,
                        refusal: QuestRefusalPolicy.RefusalClosesOffer,
                        abandonment: QuestAbandonmentPolicy.AllowedKeepsCapacityReserved,
                        capacity: 2,
                        offerDuration: 5d,
                        authorityRequirements: new[] { "authority.prototype.city.quest-assign" },
                        eligibilityGroups: new[] { All("civic-authorized", Office("office.prototype.city-investigator"), Citizenship("government.prototype.city")) });
                    break;
                case HiddenDungeonRumorDefinitionId:
                    definition.DevelopmentConfigureParticipation(
                        assignment: QuestAssignmentPolicy.Exclusive,
                        consent: QuestConsentPolicy.ExplicitRecipientConsentRequired,
                        refusal: QuestRefusalPolicy.MayReoffer,
                        abandonment: QuestAbandonmentPolicy.AllowedReleasesCapacity,
                        capacity: 1,
                        offerDuration: -1d,
                        prevalidateOffers: false,
                        eligibilityGroups: new[] { Any("rumor-discovered", Knowledge("subject.prototype.hidden-dungeon"), History("history.prototype.heard-dungeon-rumor")) });
                    break;
                case DynamicBountyDefinitionId:
                    definition.DevelopmentConfigureParticipation(
                        assignment: QuestAssignmentPolicy.CapacityLimited,
                        consent: QuestConsentPolicy.ExplicitRecipientConsentRequired,
                        refusal: QuestRefusalPolicy.MayReoffer,
                        abandonment: QuestAbandonmentPolicy.AllowedReleasesCapacity,
                        capacity: 4,
                        offerDuration: 2d,
                        authorityRequirements: new[] { "authority.prototype.bounty-board.post" },
                        eligibilityGroups: new[] { All("bounty-board", Interaction("interaction-point.prototype.bounty-board")) });
                    break;
                default:
                    definition.DevelopmentConfigureParticipation();
                    break;
            }
        }

        private static QuestEligibilityRequirementGroupData All(string id, params QuestEligibilityRequirementData[] requirements)
        {
            return Group(id, QuestEligibilityGroupPolicy.All, requirements);
        }

        private static QuestEligibilityRequirementGroupData Any(string id, params QuestEligibilityRequirementData[] requirements)
        {
            return Group(id, QuestEligibilityGroupPolicy.Any, requirements);
        }

        private static QuestEligibilityRequirementGroupData Group(string id, QuestEligibilityGroupPolicy policy, params QuestEligibilityRequirementData[] requirements)
        {
            return new QuestEligibilityRequirementGroupData
            {
                groupId = id,
                policy = policy,
                requirements = requirements ?? Array.Empty<QuestEligibilityRequirementData>()
            };
        }

        private static QuestEligibilityRequirementData Membership(string id) => Requirement(QuestEligibilityRequirementKind.OrganizationMembership, id);
        private static QuestEligibilityRequirementData Interaction(string id) => Requirement(QuestEligibilityRequirementKind.InteractionPointPresence, id);
        private static QuestEligibilityRequirementData Office(string id) => Requirement(QuestEligibilityRequirementKind.Office, id);
        private static QuestEligibilityRequirementData Citizenship(string id) => Requirement(QuestEligibilityRequirementKind.Citizenship, id);
        private static QuestEligibilityRequirementData Knowledge(string id) => Requirement(QuestEligibilityRequirementKind.Knowledge, id);
        private static QuestEligibilityRequirementData History(string id) => Requirement(QuestEligibilityRequirementKind.WorldHistoryFact, id);

        private static QuestEligibilityRequirementData Requirement(QuestEligibilityRequirementKind kind, string id)
        {
            return new QuestEligibilityRequirementData
            {
                requirementId = $"{kind}.{id}",
                kind = kind,
                requiredId = id,
                comparison = QuestRequirementComparison.Exists
            };
        }
    }
}
