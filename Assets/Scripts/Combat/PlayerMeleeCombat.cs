using System;
using UnityEngine;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Input;
using UnityIsekaiGame.Inventory;

namespace UnityIsekaiGame.Combat
{
    public sealed class PlayerMeleeCombat : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerEquipment equipment;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerStamina stamina;
        [SerializeField] private Transform attackOrigin;
        [SerializeField] private LayerMask damageMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField] private MeleeWeaponData unarmedAttack = new MeleeWeaponData();

        private float nextAttackTime;

        public event Action<MeleeAttackResult> AttackResolved;

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (equipment == null)
            {
                equipment = GetComponent<PlayerEquipment>();
            }

            if (stats == null)
            {
                stats = GetComponent<PlayerStats>();
            }

            if (stamina == null)
            {
                stamina = GetComponent<PlayerStamina>();
            }

            if (attackOrigin == null && Camera.main != null)
            {
                attackOrigin = Camera.main.transform;
            }
        }

        private void OnValidate()
        {
            unarmedAttack?.Validate();
        }

        private void Update()
        {
            if (input == null || !input.ConsumeAttack())
            {
                return;
            }

            TryAttack();
        }

        public MeleeAttackResult TryAttack()
        {
            if (Time.time < nextAttackTime)
            {
                return Resolve(MeleeAttackResult.Failure("Attack is on cooldown."));
            }

            if (attackOrigin == null)
            {
                return Resolve(MeleeAttackResult.Failure("No melee attack origin is assigned."));
            }

            MeleeWeaponData weapon = GetCurrentWeaponData(out ItemDefinition equippedItem, out bool hasEquippedMainHandItem);
            if (weapon == null || !weapon.IsWeapon)
            {
                string message = hasEquippedMainHandItem && equippedItem != null
                    ? $"{equippedItem.DisplayName} is not a melee weapon."
                    : "No melee weapon or unarmed attack is configured.";
                return Resolve(MeleeAttackResult.Failure(message));
            }

            float staminaCost = weapon.StaminaCost;
            if (staminaCost > 0f && stamina != null && !stamina.CanSpend(staminaCost))
            {
                return Resolve(MeleeAttackResult.Failure("Not enough stamina to attack."));
            }

            if (staminaCost > 0f && stamina != null)
            {
                VitalChangeResult spendResult = stamina.Spend(staminaCost, "Attack");
                if (!spendResult.Succeeded)
                {
                    return Resolve(MeleeAttackResult.Failure(spendResult.Message));
                }
            }

            nextAttackTime = Time.time + weapon.AttackCooldown;
            float damageAmount = CombatStatUtility.CalculatePreMitigationDamage(
                weapon.BaseDamage,
                gameObject,
                AttackPowerScalingPolicy.AddSourceAttackPower);
            MeleeAttackResult result = PerformHitTest(weapon, damageAmount);
            return Resolve(result);
        }

        public void ResetCooldown()
        {
            nextAttackTime = 0f;
        }

        private MeleeAttackResult PerformHitTest(MeleeWeaponData weapon, float damageAmount)
        {
            Vector3 origin = attackOrigin.position;
            Vector3 direction = attackOrigin.forward;
            RaycastHit[] hits = Physics.SphereCastAll(origin, weapon.HitRadius, direction, weapon.AttackRange, damageMask, triggerInteraction);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable == null)
                {
                    continue;
                }

                Vector3 hitDirection = hit.point == Vector3.zero ? direction : (hit.point - origin).normalized;
                DamageInfo damageInfo = new DamageInfo(damageAmount, gameObject, hit.point, hitDirection, DamageType.Physical);
                DamageResult damageResult = damageable.ApplyDamage(in damageInfo);
                string message = damageResult.Applied
                    ? $"{weapon.AttackName} hit {hit.collider.name} for {damageResult.AppliedAmount:0.#} damage."
                    : damageResult.Message;
                return MeleeAttackResult.Hit(weapon.AttackName, damageAmount, hit.collider.gameObject, damageResult, message);
            }

            return MeleeAttackResult.Miss(weapon.AttackName, damageAmount, $"{weapon.AttackName} missed.");
        }

        private MeleeWeaponData GetCurrentWeaponData(out ItemDefinition equippedItem, out bool hasEquippedMainHandItem)
        {
            equippedItem = null;
            hasEquippedMainHandItem = false;

            EquipmentSlotState mainHand = equipment == null ? null : equipment.GetSlot(EquipmentSlotType.MainHand);
            if (mainHand != null && !mainHand.IsEmpty)
            {
                equippedItem = mainHand.Item;
                hasEquippedMainHandItem = true;
                return equippedItem != null && equippedItem.IsEquippable ? equippedItem.Equipment.MeleeWeapon : null;
            }

            return unarmedAttack;
        }

        private MeleeAttackResult Resolve(MeleeAttackResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                Debug.Log(result.Message);
            }

            AttackResolved?.Invoke(result);
            return result;
        }
    }
}
