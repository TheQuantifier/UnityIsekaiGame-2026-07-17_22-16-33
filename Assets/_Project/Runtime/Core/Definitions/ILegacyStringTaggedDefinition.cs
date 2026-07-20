using System.Collections.Generic;

namespace UnityIsekaiGame.GameData
{
    public interface ILegacyStringTaggedDefinition
    {
        IReadOnlyList<string> LegacyTags { get; }
        string LegacyTagLabel { get; }
    }
}
