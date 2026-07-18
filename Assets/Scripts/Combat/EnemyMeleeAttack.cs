using System;
using UnityEngine;

namespace UnityIsekaiGame.Combat
{
    public sealed class EnemyMeleeAttack : MonoBehaviour
    {
        [SerializeField] private EnemyHealth health;
        [SerializeField, Min(0f)] private float damage = 12f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.6f;
        [SerializeField, Min(0f)] private float attackCooldown = 1.25f;

        private float nextAttackTime;

        public float AttackRange => attackRange;
        public event Action<DamageResult> AttackResolved;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }
        }

        private void OnValidate()
        {
            damage = Mathf.Max(0f, damage);
            attackRange = Mathf.Max(0.1f, attackRange);
            attackCooldown = Mathf.Max(0f, attackCooldown);
        }

        public bool CanAttack(Transform target)
        {
            return target != null
                && (health == null || !health.IsDefeated)
                && Time.time >= nextAttackTime
                && GetPlanarDistanceTo(target) <= attackRange;
        }

        public DamageResult TryAttack(Transform target)
        {
            if (target == null)
            {
                return Resolve(DamageResult.Failure(damage, "Enemy attack has no target."));
            }

            if (health != null && health.IsDefeated)
            {
                return Resolve(DamageResult.Failure(damage, $"{name} is defeated and cannot attack."));
            }

            if (Time.time < nextAttackTime)
            {
                return Resolve(DamageResult.Failure(damage, "Enemy attack is on cooldown."));
            }

            float distance = GetPlanarDistanceTo(target);
            if (distance > attackRange)
            {
                return Resolve(DamageResult.Failure(damage, "Enemy target is outside attack range."));
            }

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                damageable = target.GetComponentInChildren<IDamageable>();
            }

            if (damageable == null)
            {
                return Resolve(DamageResult.Failure(damage, "Enemy target is not damageable."));
            }

            nextAttackTime = Time.time + attackCooldown;
            Vector3 direction = GetPlanarDirectionTo(target);
            DamageInfo damageInfo = new DamageInfo(damage, gameObject, target.position, direction, DamageType.Physical);
            DamageResult result = damageable.ApplyDamage(in damageInfo);
            Debug.Log(result.Applied ? $"{name} attacked {target.name} for {result.AppliedAmount:0.#} damage." : result.Message);
            return Resolve(result);
        }

        private float GetPlanarDistanceTo(Transform target)
        {
            return GetPlanarDirectionOffset(target).magnitude;
        }

        private Vector3 GetPlanarDirectionTo(Transform target)
        {
            Vector3 offset = GetPlanarDirectionOffset(target);
            return offset.sqrMagnitude <= 0.0001f ? transform.forward : offset.normalized;
        }

        private Vector3 GetPlanarDirectionOffset(Transform target)
        {
            Vector3 offset = target.position - transform.position;
            offset.y = 0f;
            return offset;
        }

        private DamageResult Resolve(DamageResult result)
        {
            AttackResolved?.Invoke(result);
            return result;
        }
    }
}
