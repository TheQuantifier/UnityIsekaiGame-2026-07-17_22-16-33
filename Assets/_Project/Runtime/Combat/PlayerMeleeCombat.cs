using System;
using UnityEngine;
using UnityIsekaiGame.ActorLifecycle;
using UnityIsekaiGame.Equipment;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Input;
using UnityIsekaiGame.Inventory;
using UnityIsekaiGame.Magic;

namespace UnityIsekaiGame.Combat
{
    public sealed class PlayerMeleeCombat : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerEquipment equipment;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerStamina stamina;
        [SerializeField] private PlayerInventory inventory;
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

            if (inventory == null)
            {
                inventory = GetComponent<PlayerInventory>();
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
            if (!ActorLifecycleUtility.CanAct(gameObject))
            {
                return Resolve(MeleeAttackResult.Failure("Cannot attack while defeated, unconscious, or dead."));
            }

            if (Time.time < nextAttackTime)
            {
                return Resolve(MeleeAttackResult.Failure("Attack is on cooldown."));
            }

            if (attackOrigin == null)
            {
                return Resolve(MeleeAttackResult.Failure("No melee attack origin is assigned."));
            }

            CombatWeaponSelection selection = GetCurrentWeaponData();
            if (!selection.HasWeapon)
            {
                string message = selection.HasEquippedMainHandItem && selection.EquippedItem != null
                    ? $"{selection.EquippedItem.DisplayName} is not a weapon."
                    : "No melee weapon or unarmed attack is configured.";
                return Resolve(MeleeAttackResult.Failure(message));
            }

            float staminaCost = selection.StaminaCost;
            if (staminaCost > 0f && stamina != null && !stamina.CanSpend(staminaCost))
            {
                return Resolve(MeleeAttackResult.Failure("Not enough stamina to attack."));
            }

            if (selection.IsRanged && selection.RangedWeapon.AmmoItem != null)
            {
                if (inventory == null)
                {
                    return Resolve(MeleeAttackResult.Failure("No inventory is assigned for ranged ammunition."));
                }

                if (inventory.CountItem(selection.RangedWeapon.AmmoItem) <= 0)
                {
                    return Resolve(MeleeAttackResult.Failure($"No {selection.RangedWeapon.AmmoItem.DisplayName} available."));
                }
            }

            if (staminaCost > 0f && stamina != null)
            {
                VitalChangeResult spendResult = stamina.Spend(staminaCost, "Attack");
                if (!spendResult.Succeeded)
                {
                    return Resolve(MeleeAttackResult.Failure(spendResult.Message));
                }
            }

            if (selection.IsRanged && selection.RangedWeapon.AmmoItem != null && inventory != null)
            {
                if (!inventory.RemoveItem(selection.RangedWeapon.AmmoItem, 1))
                {
                    return Resolve(MeleeAttackResult.Failure($"Could not consume {selection.RangedWeapon.AmmoItem.DisplayName}."));
                }
            }

            nextAttackTime = Time.time + selection.AttackCooldown;
            float damageAmount = CombatStatUtility.CalculatePreMitigationDamage(
                selection.BaseDamage,
                gameObject,
                AttackPowerScalingPolicy.AddSourceAttackPower);
            MeleeAttackResult result = selection.IsRanged
                ? FireProjectile(selection.RangedWeapon, damageAmount)
                : PerformHitTest(selection.MeleeWeapon, damageAmount);
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
                if (damageable == null && hit.collider.GetComponentInParent<UnityIsekaiGame.ResourceSystem.CharacterResourceCollection>() == null)
                {
                    continue;
                }

                Vector3 hitDirection = hit.point == Vector3.zero ? direction : (hit.point - origin).normalized;
                DamageComponent component = weapon.DamageType == null
                    ? DamageComponent.Legacy(DamageType.Physical, damageAmount, AttackPowerScalingPolicy.AddSourceAttackPower)
                    : new DamageComponent(weapon.DamageType, damageAmount, AttackPowerScalingPolicy.AddSourceAttackPower);
                DamagePacket packet = DamagePacket.Single(gameObject, component);
                DamageInfo damageInfo = new DamageInfo(damageAmount, gameObject, hit.point, hitDirection, DamageType.Physical, packet);
                DamageResult damageResult = SceneCombatDamageBridge.ApplyDamage(
                    hit.collider.gameObject,
                    in damageInfo,
                    $"player-melee.{weapon.AttackName}",
                    weapon.AttackName);
                string message = damageResult.Applied
                    ? $"{weapon.AttackName} hit {hit.collider.name} for {damageResult.AppliedAmount:0.#} damage."
                    : damageResult.Message;
                return MeleeAttackResult.Hit(weapon.AttackName, damageAmount, hit.collider.gameObject, damageResult, message);
            }

            return MeleeAttackResult.Miss(weapon.AttackName, damageAmount, $"{weapon.AttackName} missed.");
        }

        private MeleeAttackResult FireProjectile(RangedWeaponData weapon, float damageAmount)
        {
            Vector3 origin = attackOrigin.TransformPoint(weapon.LaunchOffset);
            Vector3 direction = attackOrigin.forward.sqrMagnitude > 0f ? attackOrigin.forward.normalized : transform.forward;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            SpellProjectile projectile = weapon.ProjectilePrefab == null
                ? CreateRuntimeProjectile(origin, rotation, weapon)
                : Instantiate(weapon.ProjectilePrefab, origin, rotation);

            if (projectile == null)
            {
                return MeleeAttackResult.Failure("Invalid ranged projectile configuration.");
            }

            if (weapon.ProjectileVisualPrefab != null)
            {
                GameObject visual = Instantiate(weapon.ProjectileVisualPrefab, projectile.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }

            projectile.Initialize(
                gameObject,
                direction,
                weapon.ProjectileSpeed,
                damageAmount,
                weapon.DamageType,
                weapon.ProjectileLifetime,
                $"player-ranged.{weapon.AttackName}",
                weapon.AttackName);
            return MeleeAttackResult.Miss(weapon.AttackName, damageAmount, $"{weapon.AttackName} fired.");
        }

        private static SpellProjectile CreateRuntimeProjectile(Vector3 origin, Quaternion rotation, RangedWeaponData weapon)
        {
            GameObject projectileObject = new GameObject($"{weapon.AttackName} Projectile");
            projectileObject.transform.SetPositionAndRotation(origin, rotation);
            SphereCollider collider = projectileObject.AddComponent<SphereCollider>();
            collider.radius = weapon.ProjectileHitRadius;
            Rigidbody body = projectileObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            SpellProjectile projectile = projectileObject.AddComponent<SpellProjectile>();
            return projectile;
        }

        private CombatWeaponSelection GetCurrentWeaponData()
        {
            ItemDefinition equippedItem = null;
            bool hasEquippedMainHandItem = false;

            EquipmentSlotState mainHand = equipment == null ? null : equipment.GetSlot(EquipmentSlotType.MainHand);
            if (mainHand != null && !mainHand.IsEmpty)
            {
                equippedItem = mainHand.Item;
                hasEquippedMainHandItem = true;
                if (equippedItem == null || !equippedItem.IsEquippable)
                {
                    return CombatWeaponSelection.Invalid(equippedItem, hasEquippedMainHandItem);
                }

                RangedWeaponData ranged = equippedItem.Equipment.RangedWeapon;
                if (ranged != null && ranged.IsWeapon)
                {
                    return CombatWeaponSelection.Ranged(equippedItem, hasEquippedMainHandItem, ranged);
                }

                MeleeWeaponData melee = equippedItem.Equipment.MeleeWeapon;
                return melee != null && melee.IsWeapon
                    ? CombatWeaponSelection.Melee(equippedItem, hasEquippedMainHandItem, melee)
                    : CombatWeaponSelection.Invalid(equippedItem, hasEquippedMainHandItem);
            }

            return unarmedAttack != null && unarmedAttack.IsWeapon
                ? CombatWeaponSelection.Melee(null, false, unarmedAttack)
                : CombatWeaponSelection.Invalid(null, false);
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

        private readonly struct CombatWeaponSelection
        {
            private CombatWeaponSelection(
                ItemDefinition equippedItem,
                bool hasEquippedMainHandItem,
                MeleeWeaponData meleeWeapon,
                RangedWeaponData rangedWeapon)
            {
                EquippedItem = equippedItem;
                HasEquippedMainHandItem = hasEquippedMainHandItem;
                MeleeWeapon = meleeWeapon;
                RangedWeapon = rangedWeapon;
            }

            public ItemDefinition EquippedItem { get; }
            public bool HasEquippedMainHandItem { get; }
            public MeleeWeaponData MeleeWeapon { get; }
            public RangedWeaponData RangedWeapon { get; }
            public bool IsRanged => RangedWeapon != null && RangedWeapon.IsWeapon;
            public bool HasWeapon => IsRanged || (MeleeWeapon != null && MeleeWeapon.IsWeapon);
            public float StaminaCost => IsRanged ? RangedWeapon.StaminaCost : MeleeWeapon.StaminaCost;
            public float AttackCooldown => IsRanged ? RangedWeapon.AttackCooldown : MeleeWeapon.AttackCooldown;
            public float BaseDamage => IsRanged ? RangedWeapon.BaseDamage : MeleeWeapon.BaseDamage;

            public static CombatWeaponSelection Invalid(ItemDefinition equippedItem, bool hasEquippedMainHandItem)
            {
                return new CombatWeaponSelection(equippedItem, hasEquippedMainHandItem, null, null);
            }

            public static CombatWeaponSelection Melee(ItemDefinition equippedItem, bool hasEquippedMainHandItem, MeleeWeaponData weapon)
            {
                return new CombatWeaponSelection(equippedItem, hasEquippedMainHandItem, weapon, null);
            }

            public static CombatWeaponSelection Ranged(ItemDefinition equippedItem, bool hasEquippedMainHandItem, RangedWeaponData weapon)
            {
                return new CombatWeaponSelection(equippedItem, hasEquippedMainHandItem, null, weapon);
            }
        }
    }
}
