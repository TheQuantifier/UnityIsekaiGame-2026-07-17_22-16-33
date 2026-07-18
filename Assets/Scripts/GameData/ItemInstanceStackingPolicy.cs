namespace UnityIsekaiGame.GameData
{
    public static class ItemInstanceStackingPolicy
    {
        public static bool CanShareStack(ItemInstance left, ItemInstance right)
        {
            if (left == null || right == null || left.Definition == null || right.Definition == null)
            {
                return false;
            }

            if (!ReferenceEquals(left.Definition, right.Definition) || !left.Definition.Stackable)
            {
                return false;
            }

            if (left.HasPersistentIdentity || right.HasPersistentIdentity)
            {
                return false;
            }

            if (left.RequiresPersistentIdentity || right.RequiresPersistentIdentity)
            {
                return false;
            }

            return ItemInstanceMetadata.AreEquivalentForStacking(left.Metadata, right.Metadata);
        }
    }
}
