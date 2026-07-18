using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.People
{
    [CreateAssetMenu(fileName = "Person", menuName = "Unity Isekai Game/People/Person")]
    public sealed class PersonDefinition : ScriptableObject, IGameDefinition, ICategorizableDefinition, ITaggedDefinition, ILegacyStringTaggedDefinition
    {
        [SerializeField] private string personId;
        [SerializeField] private string displayName;
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 4)] private string shortDescription;
        [SerializeField] private Sprite portrait;
        [SerializeField] private CategoryDefinition primaryCategory;
        [SerializeField] private TagDefinition[] tags;
        [SerializeField] private string[] roleTags;
        [SerializeField] private string factionIdPlaceholder;
        [SerializeField] private string settlementIdPlaceholder;
        [SerializeField] private PersonImportance importance = PersonImportance.Standard;

        public string PersonId => personId;
        public string Id => personId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Title => title;
        public string ShortDescription => shortDescription;
        public Sprite Portrait => portrait;
        public CategoryDefinition PrimaryCategory => primaryCategory;
        public CategoryDomain ClassificationDomain => CategoryDomain.Person;
        public IReadOnlyList<TagDefinition> Tags => tags ?? Array.Empty<TagDefinition>();
        public IReadOnlyList<string> RoleTags => roleTags ?? Array.Empty<string>();
        public IReadOnlyList<string> LegacyTags => RoleTags;
        public string LegacyTagLabel => "role";
        public string FactionIdPlaceholder => factionIdPlaceholder;
        public string SettlementIdPlaceholder => settlementIdPlaceholder;
        public PersonImportance Importance => importance;
        public bool HasValidPersonId => !string.IsNullOrWhiteSpace(personId);
    }
}
