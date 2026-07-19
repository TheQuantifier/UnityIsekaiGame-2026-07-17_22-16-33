using System.Collections.Generic;
using System.Text;
using UnityIsekaiGame.GameData;

namespace UnityIsekaiGame.Places
{
    public static class PlaceHierarchyUtility
    {
        public static bool HasSameId(PlaceDefinition left, PlaceDefinition right)
        {
            return left != null
                && right != null
                && !string.IsNullOrWhiteSpace(left.Id)
                && left.Id == right.Id;
        }

        public static bool IsDescendantOf(PlaceDefinition place, PlaceDefinition ancestor)
        {
            if (place == null || ancestor == null)
            {
                return false;
            }

            HashSet<string> visitedIds = new HashSet<string>();
            PlaceDefinition current = place.ParentPlace;
            while (current != null && visitedIds.Add(current.Id))
            {
                if (HasSameId(current, ancestor))
                {
                    return true;
                }

                current = current.ParentPlace;
            }

            return false;
        }

        public static bool ContainsOrIs(PlaceDefinition place, PlaceDefinition possibleAncestor)
        {
            return HasSameId(place, possibleAncestor) || IsDescendantOf(place, possibleAncestor);
        }

        public static IReadOnlyList<PlaceDefinition> GetAncestors(PlaceDefinition place)
        {
            List<PlaceDefinition> ancestors = new List<PlaceDefinition>();
            if (place == null)
            {
                return ancestors;
            }

            HashSet<string> visitedIds = new HashSet<string>();
            PlaceDefinition current = place.ParentPlace;
            while (current != null && visitedIds.Add(current.Id))
            {
                ancestors.Add(current);
                current = current.ParentPlace;
            }

            return ancestors;
        }

        public static PlaceDefinition FindNearestAncestorOfKind(PlaceDefinition place, PlaceKind kind, bool includeSelf = true)
        {
            if (place == null)
            {
                return null;
            }

            HashSet<string> visitedIds = new HashSet<string>();
            PlaceDefinition current = includeSelf ? place : place.ParentPlace;
            while (current != null && visitedIds.Add(current.Id))
            {
                if (current.PlaceKind == kind)
                {
                    return current;
                }

                current = current.ParentPlace;
            }

            return null;
        }

        public static PlaceDefinition FindNearestAncestorInCategory(PlaceDefinition place, string categoryId, bool includeSelf = true)
        {
            if (place == null || string.IsNullOrWhiteSpace(categoryId))
            {
                return null;
            }

            HashSet<string> visitedIds = new HashSet<string>();
            PlaceDefinition current = includeSelf ? place : place.ParentPlace;
            while (current != null && visitedIds.Add(current.Id))
            {
                if (ClassificationUtility.IsInCategory(current, categoryId))
                {
                    return current;
                }

                current = current.ParentPlace;
            }

            return null;
        }

        public static PlaceDefinition GetContainingSettlement(PlaceDefinition place)
        {
            return FindNearestAncestorOfKind(place, PlaceKind.Settlement);
        }

        public static PlaceDefinition GetContainingRegion(PlaceDefinition place)
        {
            return FindNearestAncestorOfKind(place, PlaceKind.Region);
        }

        public static PlaceDefinition GetContainingNation(PlaceDefinition place)
        {
            return FindNearestAncestorOfKind(place, PlaceKind.Nation);
        }

        public static bool HasParentCycle(PlaceDefinition place)
        {
            if (place == null)
            {
                return false;
            }

            HashSet<string> visitedIds = new HashSet<string>();
            PlaceDefinition current = place;
            while (current != null)
            {
                if (!visitedIds.Add(current.Id))
                {
                    return true;
                }

                current = current.ParentPlace;
            }

            return false;
        }

        public static string GetHierarchyPath(PlaceDefinition place)
        {
            if (place == null)
            {
                return string.Empty;
            }

            List<PlaceDefinition> hierarchy = new List<PlaceDefinition> { place };
            hierarchy.AddRange(GetAncestors(place));
            hierarchy.Reverse();

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hierarchy.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" / ");
                }

                builder.Append(hierarchy[i].DisplayName);
            }

            return builder.ToString();
        }

        public static string GetExpectedCategoryId(PlaceKind kind)
        {
            switch (kind)
            {
                case PlaceKind.World:
                    return "place-category.world";
                case PlaceKind.Nation:
                    return "place-category.nation";
                case PlaceKind.Region:
                    return "place-category.region";
                case PlaceKind.Settlement:
                    return "place-category.settlement";
                case PlaceKind.District:
                    return "place-category.district";
                case PlaceKind.Building:
                    return "place-category.building";
                case PlaceKind.Interior:
                    return "place-category.interior";
                case PlaceKind.Dungeon:
                    return "place-category.dungeon";
                case PlaceKind.Wilderness:
                    return "place-category.wilderness";
                case PlaceKind.Route:
                    return "place-category.route";
                case PlaceKind.PointOfInterest:
                    return "place-category.point-of-interest";
                default:
                    return string.Empty;
            }
        }
    }
}
