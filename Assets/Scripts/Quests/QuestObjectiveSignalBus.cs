using System;

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
    }
}
