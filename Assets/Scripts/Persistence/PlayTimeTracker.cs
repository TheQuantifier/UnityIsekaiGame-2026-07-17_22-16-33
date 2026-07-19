using UnityEngine;

namespace UnityIsekaiGame.Persistence
{
    public sealed class PlayTimeTracker : MonoBehaviour
    {
        [SerializeField, Min(0f)] private double cumulativeSeconds;
        [SerializeField] private bool countWhileMenuOpen = true;

        private bool paused;

        public double CumulativeSeconds => cumulativeSeconds;
        public bool CountWhileMenuOpen => countWhileMenuOpen;

        private void Update()
        {
            if (paused)
            {
                return;
            }

            cumulativeSeconds += Time.unscaledDeltaTime;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            paused = pauseStatus;
        }

        public void Restore(double seconds)
        {
            cumulativeSeconds = System.Math.Max(0d, seconds);
        }
    }
}
