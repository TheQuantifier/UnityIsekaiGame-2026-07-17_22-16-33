using System.Collections.Generic;
using System.Linq;
using UnityIsekaiGame.Knowledge.Access;

namespace UnityIsekaiGame.Quests
{
    public static class QuestInformationSubject
    {
        public const string QuestTag = "quest";
        public const string QuestDefinitionTag = "quest-definition";

        public static InformationSubjectReferenceData Quest(string questId, string questDefinitionId, string ownerPersonId = "", string controllingEntityId = "", IEnumerable<string> tags = null)
        {
            return Create(questId, questDefinitionId, ownerPersonId, controllingEntityId, QuestTag, tags);
        }

        public static InformationSubjectReferenceData Definition(string questDefinitionId, IEnumerable<string> tags = null)
        {
            return Create(questDefinitionId, string.Empty, string.Empty, string.Empty, QuestDefinitionTag, tags);
        }

        private static InformationSubjectReferenceData Create(string subjectId, string parentSubjectId, string ownerPersonId, string controllingEntityId, string primaryTag, IEnumerable<string> tags)
        {
            return new InformationSubjectReferenceData
            {
                subjectType = InformationSubjectType.Custom,
                subjectId = subjectId ?? string.Empty,
                parentSubjectId = parentSubjectId ?? string.Empty,
                ownerPersonId = ownerPersonId ?? string.Empty,
                controllingEntityId = controllingEntityId ?? string.Empty,
                tags = QuestRuntimeModelUtility.Clean((tags ?? Enumerable.Empty<string>()).Concat(new[] { primaryTag }))
            };
        }
    }
}
