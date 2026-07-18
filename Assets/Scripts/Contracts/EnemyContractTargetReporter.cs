using UnityEngine;
using UnityIsekaiGame.Combat;

namespace UnityIsekaiGame.Contracts
{
    public sealed class EnemyContractTargetReporter : MonoBehaviour
    {
        [SerializeField] private EnemyHealth health;
        [SerializeField] private ContractObjectiveTarget target;
        [SerializeField] private PlayerContractJournal journal;

        private bool reportedDefeat;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            if (target == null)
            {
                target = GetComponent<ContractObjectiveTarget>();
            }

            if (journal == null)
            {
                journal = FindAnyObjectByType<PlayerContractJournal>();
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Defeated += OnDefeated;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Defeated -= OnDefeated;
            }
        }

        public void ResetReporter()
        {
            reportedDefeat = false;
        }

        private void OnDefeated()
        {
            if (reportedDefeat)
            {
                return;
            }

            reportedDefeat = true;
            journal?.RecordDefeat(target);
        }
    }
}
