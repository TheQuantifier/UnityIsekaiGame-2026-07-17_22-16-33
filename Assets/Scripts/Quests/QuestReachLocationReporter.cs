using UnityEngine;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Places;

namespace UnityIsekaiGame.Quests
{
    public sealed class QuestReachLocationReporter : MonoBehaviour
    {
        [SerializeField] private PlaceDefinition targetPlace;
        [SerializeField] private string locationId;
        [SerializeField] private bool reportOnce;
        [SerializeField] private bool showPrototypeHudMessage = true;
        [SerializeField] private string prototypeHudMessage;
        [SerializeField, Min(0.1f)] private float prototypeHudMessageCooldown = 1.5f;

        public PlaceDefinition TargetPlace => targetPlace;
        public string ReportedLocationId => targetPlace == null ? locationId : targetPlace.Id;
        public string DisplayName => targetPlace == null ? ReportedLocationId : targetPlace.DisplayName;

        private bool reported;
        private float nextPrototypeHudMessageTime;

        private void OnTriggerEnter(Collider other)
        {
            TryReport(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryReport(other);
        }

        private void TryReport(Collider other)
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
            if (showPrototypeHudMessage && Time.time >= nextPrototypeHudMessageTime)
            {
                string message = string.IsNullOrWhiteSpace(prototypeHudMessage)
                    ? $"Entered {DisplayName}"
                    : prototypeHudMessage;
                PrototypeHudMessageBus.Show(message);
                nextPrototypeHudMessageTime = Time.time + prototypeHudMessageCooldown;
            }

            QuestObjectiveSignalBus.ReportReachLocation(ReportedLocationId);
        }

        public void ResetReporter()
        {
            reported = false;
        }
    }
}
