using UnityEngine;

namespace UnityIsekaiGame.Quests
{
    public sealed class QuestReachLocationReporter : MonoBehaviour
    {
        [SerializeField] private string locationId;
        [SerializeField] private bool reportOnce;

        private bool reported;

        private void OnTriggerEnter(Collider other)
        {
            if (reported && reportOnce)
            {
                return;
            }

            if (other == null || other.GetComponentInParent<PlayerQuestLog>() == null)
            {
                return;
            }

            reported = true;
            QuestObjectiveSignalBus.ReportReachLocation(locationId);
        }

        public void ResetReporter()
        {
            reported = false;
        }
    }
}
