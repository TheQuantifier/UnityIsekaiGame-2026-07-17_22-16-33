using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Knowledge.Access;
using UnityIsekaiGame.Knowledge.Records;

namespace UnityIsekaiGame.Gameplay
{
    public static class PrototypeKnowledgeRecordDefinitionFactory
    {
        public static IReadOnlyList<KnowledgeRecordDefinition> CreateKnowledgeRecordDefinitions()
        {
            return new[]
            {
                RecordDefinition("record-definition.journal-entry", "Journal Entry", KnowledgeRecordCategory.PersonalJournal, new[] { InformationSubjectType.HistoricalEvent, InformationSubjectType.Claim, InformationSubjectType.Memory }, new[] { KnowledgeRecordOwnerKind.Person }),
                RecordDefinition("record-definition.historical-record", "Historical Record", KnowledgeRecordCategory.HistoricalRecord, new[] { InformationSubjectType.HistoricalEvent, InformationSubjectType.LifeEvent }, new[] { KnowledgeRecordOwnerKind.Person, KnowledgeRecordOwnerKind.PublicWorldRecord }),
                RecordDefinition("record-definition.biography-entry", "Biography Entry", KnowledgeRecordCategory.Biography, new[] { InformationSubjectType.PersonIdentity, InformationSubjectType.LifeEvent }, new[] { KnowledgeRecordOwnerKind.Person }),
                RecordDefinition("record-definition.bestiary-entry", "Bestiary Entry", KnowledgeRecordCategory.Bestiary, new[] { InformationSubjectType.BodyIdentity, InformationSubjectType.Custom }, new[] { KnowledgeRecordOwnerKind.Person, KnowledgeRecordOwnerKind.SharedArchive }),
                RecordDefinition("record-definition.location-entry", "Location Entry", KnowledgeRecordCategory.LocationRecord, new[] { InformationSubjectType.Location }, new[] { KnowledgeRecordOwnerKind.Person, KnowledgeRecordOwnerKind.PublicWorldRecord }),
                RecordDefinition("record-definition.medical-record", "Medical Record", KnowledgeRecordCategory.MedicalRecord, new[] { InformationSubjectType.Diagnosis, InformationSubjectType.Condition, InformationSubjectType.Disease }, new[] { KnowledgeRecordOwnerKind.Person, KnowledgeRecordOwnerKind.Organization }),
                RecordDefinition("record-definition.investigation-record", "Investigation Record", KnowledgeRecordCategory.InvestigationRecord, new[] { InformationSubjectType.SourceChain, InformationSubjectType.Evidence, InformationSubjectType.Claim }, new[] { KnowledgeRecordOwnerKind.Person, KnowledgeRecordOwnerKind.Organization }),
                RecordDefinition("record-definition.organization-entry", "Organization Entry", KnowledgeRecordCategory.OrganizationRecord, new[] { InformationSubjectType.Organization, InformationSubjectType.Affiliation }, new[] { KnowledgeRecordOwnerKind.Person, KnowledgeRecordOwnerKind.Organization }),
                RecordDefinition("record-definition.quest-note", "Quest Note", KnowledgeRecordCategory.QuestRelatedRecord, new[] { InformationSubjectType.Custom, InformationSubjectType.Claim }, new[] { KnowledgeRecordOwnerKind.Person }),
                RecordDefinition("record-definition.custom-entry", "Custom Entry", KnowledgeRecordCategory.Custom, new[] { InformationSubjectType.Custom, InformationSubjectType.Document, InformationSubjectType.Claim }, new[] { KnowledgeRecordOwnerKind.Person, KnowledgeRecordOwnerKind.SharedArchive })
            };
        }

        public static IEnumerable<KnowledgeRecordDefinition> CreateMissingKnowledgeRecordDefinitions(IEnumerable<string> existingIds)
        {
            HashSet<string> knownIds = new HashSet<string>(existingIds ?? System.Array.Empty<string>(), System.StringComparer.Ordinal);
            foreach (KnowledgeRecordDefinition definition in CreateKnowledgeRecordDefinitions())
            {
                if (definition != null && knownIds.Add(definition.Id))
                {
                    yield return definition;
                }
            }
        }

        private static KnowledgeRecordDefinition RecordDefinition(string id, string displayName, KnowledgeRecordCategory category, InformationSubjectType[] subjectTypes, KnowledgeRecordOwnerKind[] ownerKinds)
        {
            KnowledgeRecordDefinition definition = ScriptableObject.CreateInstance<KnowledgeRecordDefinition>();
            definition.DevelopmentConfigure(id, displayName, category, subjectTypes, ownerKinds, KnowledgeRecordProjectionKind.ExplicitRecord, KnowledgeRecordPersistencePolicy.ExplicitOnly);
            return definition;
        }
    }
}
