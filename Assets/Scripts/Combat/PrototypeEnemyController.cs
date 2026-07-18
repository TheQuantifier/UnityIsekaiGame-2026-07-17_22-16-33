using UnityEngine;

namespace UnityIsekaiGame.Combat
{
    public sealed class PrototypeEnemyController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyMeleeAttack meleeAttack;
        [SerializeField, Min(0.1f)] private float detectionRadius = 7f;
        [SerializeField, Min(0f)] private float moveSpeed = 2f;
        [SerializeField, Min(0.1f)] private float stoppingDistance = 1.35f;
        [SerializeField, Min(0f)] private float turnSpeed = 12f;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            if (meleeAttack == null)
            {
                meleeAttack = GetComponent<EnemyMeleeAttack>();
            }
        }

        private void OnValidate()
        {
            detectionRadius = Mathf.Max(0.1f, detectionRadius);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            stoppingDistance = Mathf.Max(0.1f, stoppingDistance);
            turnSpeed = Mathf.Max(0f, turnSpeed);
        }

        private void Update()
        {
            if (target == null || health != null && health.IsDefeated)
            {
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance > detectionRadius)
            {
                return;
            }

            FaceTarget(toTarget);

            float attackRange = meleeAttack == null ? stoppingDistance : meleeAttack.AttackRange;
            if (distance <= attackRange)
            {
                meleeAttack?.TryAttack(target);
                return;
            }

            if (distance <= stoppingDistance || moveSpeed <= 0f)
            {
                return;
            }

            Vector3 direction = toTarget.normalized;
            float step = Mathf.Min(moveSpeed * Time.deltaTime, distance - stoppingDistance);
            transform.position += direction * step;
        }

        public void ResetControllerState()
        {
            // The prototype controller is currently stateless; this method keeps reset orchestration explicit.
        }

        private void FaceTarget(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = turnSpeed <= 0f
                ? targetRotation
                : Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}
