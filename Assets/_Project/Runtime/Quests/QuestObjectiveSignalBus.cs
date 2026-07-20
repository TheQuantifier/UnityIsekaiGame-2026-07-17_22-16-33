using System;
using UnityIsekaiGame.Places;

namespace UnityIsekaiGame.Quests
{
    public static class QuestObjectiveSignalBus
    {
        public static event Action<string> TalkedTo;
        public static event Action<string> ReachedLocation;

        public static void ReportTalk(string targetId)
        {
            if (!string.IsNullOrWhiteSpace(targetId))
            {
                TalkedTo?.Invoke(targetId);
            }
        }

        public static void ReportReachLocation(string locationId)
        {
            if (!string.IsNullOrWhiteSpace(locationId))
            {
                ReachedLocation?.Invoke(locationId);
            }
        }

        public static void ReportReachLocation(PlaceDefinition place)
        {
            if (place != null)
            {
                ReportReachLocation(place.Id);
            }
        }
    }
}
