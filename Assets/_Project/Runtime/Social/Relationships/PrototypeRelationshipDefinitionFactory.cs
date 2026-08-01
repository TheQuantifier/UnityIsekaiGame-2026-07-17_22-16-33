using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Social.Relationships
{
    public static class PrototypeRelationshipDefinitionFactory
    {
        public const string FriendRelationshipId = "relationship.prototype.friend";
        public const string ParentChildRelationshipId = "relationship.prototype.parent-child";
        public const string MentorStudentRelationshipId = "relationship.prototype.mentor-student";
        public const string RivalRelationshipId = "relationship.prototype.rival";

        public static IReadOnlyList<ScriptableObject> CreateDefinitions()
        {
            return new ScriptableObject[]
            {
                Definition(FriendRelationshipId, "Prototype Friendship", RelationshipCategory.Personal, RelationshipDirectionality.Symmetric, RelationshipDuplicatePolicy.OneActiveBetweenParticipants, true, Role("friend", "Friend")),
                Definition(ParentChildRelationshipId, "Prototype Parent Child", RelationshipCategory.Family, RelationshipDirectionality.Directed, RelationshipDuplicatePolicy.OneActivePerDefinitionAndRoles, false, Role("parent", "Parent"), Role("child", "Child")),
                Definition(MentorStudentRelationshipId, "Prototype Mentor Student", RelationshipCategory.Professional, RelationshipDirectionality.Directed, RelationshipDuplicatePolicy.OneActivePerDefinitionAndRoles, true, Role("mentor", "Mentor"), Role("student", "Student")),
                Definition(RivalRelationshipId, "Prototype Rivalry", RelationshipCategory.Conflict, RelationshipDirectionality.Symmetric, RelationshipDuplicatePolicy.OneActiveBetweenParticipants, true, Role("rival", "Rival"))
            };
        }

        public static DefinitionRegistry AddMissingPrototypeRelationshipDefinitions(DefinitionRegistry baseRegistry)
        {
            IGameDefinition[] existing = baseRegistry == null
                ? new IGameDefinition[0]
                : baseRegistry.DefinitionsById.Values.ToArray();
            IGameDefinition[] additions = CreateDefinitions()
                .OfType<IGameDefinition>()
                .Where(definition => baseRegistry == null || !baseRegistry.Contains(definition.Id))
                .ToArray();
            return new DefinitionRegistry(existing.Concat(additions));
        }

        private static RelationshipDefinition Definition(string id, string name, RelationshipCategory category, RelationshipDirectionality directionality, RelationshipDuplicatePolicy duplicatePolicy, bool mayEnd, params RelationshipRoleDefinitionData[] roles)
        {
            RelationshipDefinition definition = ScriptableObject.CreateInstance<RelationshipDefinition>();
            definition.name = name.Replace(" ", string.Empty);
            definition.DevelopmentConfigure(id, name, category, directionality, duplicatePolicy, roles, mayEnd);
            return definition;
        }

        private static RelationshipRoleDefinitionData Role(string id, string name)
        {
            return new RelationshipRoleDefinitionData
            {
                roleId = id,
                displayName = name,
                required = true
            };
        }
    }
}
