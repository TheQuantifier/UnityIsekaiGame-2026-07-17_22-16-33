using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityIsekaiGame.Abilities;
using UnityIsekaiGame.ActorLifecycle;
using UnityIsekaiGame.Gameplay;
using UnityIsekaiGame.Input;

namespace UnityIsekaiGame.Magic
{
    public sealed class PlayerSpellcaster : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMana mana;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private Transform castOrigin;
        [SerializeField] private PlayerSpellLoadout loadout;
        [SerializeField] private SpellDefinition primarySpell;
        [SerializeField] private LayerMask aimMask = ~0;
        [SerializeField] private QueryTriggerInteraction aimTriggerInteraction = QueryTriggerInteraction.Ignore;

        private readonly List<SpellProjectile> activeProjectiles = new List<SpellProjectile>();
        private readonly Dictionary<SpellDefinition, float> cooldowns = new Dictionary<SpellDefinition, float>();
        private readonly AbilityCooldownTracker abilityCooldowns = new AbilityCooldownTracker();

        public event Action<SpellDefinition, SpellCastResult> SpellCastResolved;

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (mana == null)
            {
                mana = GetComponent<PlayerMana>();
            }

            if (health == null)
            {
                health = GetComponent<PlayerHealth>();
            }

            if (loadout == null)
            {
                loadout = GetComponent<PlayerSpellLoadout>();
            }

            if (castOrigin == null && Camera.main != null)
            {
                castOrigin = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (input != null && input.ConsumeCastPrimarySpell())
            {
                TryCastPrimarySpell();
            }
        }

        public SpellCastResult TryCastPrimarySpell()
        {
            SpellDefinition spell = GetCurrentSpell();
            if (spell != null && spell.Ability != null)
            {
                return TryCastAbilitySpell(spell);
            }

            SpellCastResult validation = ValidateCast(spell);
            if (!validation.Succeeded)
            {
                ReportFailure(validation.Message);
                return validation;
            }

            VitalChangeResult manaSpend = mana.Spend(spell.ManaCost);
            if (!manaSpend.Succeeded)
            {
                ReportFailure(manaSpend.Message);
                return SpellCastResult.Failure(manaSpend.Message);
            }

            SpellProjectile projectile = SpawnProjectile(spell);
            if (projectile == null)
            {
                mana.Restore(spell.ManaCost);
                return SpellCastResult.Failure("Invalid projectile configuration.");
            }

            cooldowns[spell] = Time.time + spell.Cooldown;
            string message = $"Cast {spell.DisplayName}.";
            Debug.Log(message);
            SpellCastResult result = SpellCastResult.Success(message);
            SpellCastResolved?.Invoke(spell, result);
            return result;
        }

        public void ResetSpellcasting()
        {
            cooldowns.Clear();
            abilityCooldowns.Reset();
            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
            {
                SpellProjectile projectile = activeProjectiles[i];
                if (projectile != null)
                {
                    projectile.Completed -= HandleProjectileCompleted;
                    Destroy(projectile.gameObject);
                }
            }

            activeProjectiles.Clear();
        }

        private SpellCastResult ValidateCast(SpellDefinition spell)
        {
            if (spell == null)
            {
                return SpellCastResult.Failure("No spell assigned.");
            }

            if (input != null && input.GameplayInputBlocked)
            {
                return SpellCastResult.Failure("Gameplay input is blocked.");
            }

            if (health != null && health.IsDefeated)
            {
                return SpellCastResult.Failure("Cannot cast while defeated.");
            }

            if (!ActorLifecycleUtility.CanAct(gameObject))
            {
                return SpellCastResult.Failure("Cannot cast while defeated, unconscious, or dead.");
            }

            if (cooldowns.TryGetValue(spell, out float nextCastTime) && Time.time < nextCastTime)
            {
                return SpellCastResult.Failure($"{spell.DisplayName} is on cooldown.");
            }

            if (mana == null)
            {
                return SpellCastResult.Failure("No mana source assigned.");
            }

            if (!mana.CanSpend(spell.ManaCost))
            {
                return SpellCastResult.Failure("Not enough mana.");
            }

            if (castOrigin == null || spell.ProjectilePrefab == null)
            {
                return SpellCastResult.Failure("Invalid projectile configuration.");
            }

            return SpellCastResult.Success("Spell can be cast.");
        }

        private SpellProjectile SpawnProjectile(SpellDefinition spell)
        {
            Vector3 spawnPosition = castOrigin.TransformPoint(spell.CastPointOffset);
            Vector3 castDirection = GetCastDirection(spawnPosition, spell);
            Quaternion spawnRotation = Quaternion.LookRotation(castDirection, Vector3.up);
            SpellProjectile projectile = Instantiate(spell.ProjectilePrefab, spawnPosition, spawnRotation);
            projectile.Completed += HandleProjectileCompleted;
            projectile.Initialize(gameObject, castDirection, spell.ProjectileSpeed, spell.BaseDamage, spell.MaximumLifetime);
            activeProjectiles.Add(projectile);
            return projectile;
        }

        private SpellCastResult TryCastAbilitySpell(SpellDefinition spell)
        {
            Vector3 sourcePosition = castOrigin == null ? transform.position : castOrigin.position;
            Vector3 direction = castOrigin == null ? transform.forward : GetCastDirection(sourcePosition, spell);
            AbilityExecutionContext context = new AbilityExecutionContext(
                spell.Ability,
                gameObject,
                null,
                castOrigin,
                sourcePosition,
                sourcePosition + direction * Mathf.Max(1f, spell.Ability.Range),
                direction,
                gameplayBlocked: input != null && input.GameplayInputBlocked,
                projectileSpawned: RegisterProjectile);

            AbilityExecutionResult result = AbilityExecutor.Execute(in context, abilityCooldowns);
            if (!result.Succeeded)
            {
                ReportFailure(result.Message);
                return SpellCastResult.Failure(result.Message);
            }

            string message = $"Cast {spell.DisplayName}.";
            Debug.Log(message);
            SpellCastResult castResult = SpellCastResult.Success(message);
            SpellCastResolved?.Invoke(spell, castResult);
            return castResult;
        }

        private void RegisterProjectile(SpellProjectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            projectile.Completed += HandleProjectileCompleted;
            activeProjectiles.Add(projectile);
        }

        private Vector3 GetCastDirection(Vector3 spawnPosition, SpellDefinition spell)
        {
            Vector3 aimPoint = castOrigin.position + castOrigin.forward * (spell.ProjectileSpeed * spell.MaximumLifetime);
            RaycastHit[] hits = Physics.RaycastAll(castOrigin.position, castOrigin.forward, Vector3.Distance(castOrigin.position, aimPoint), aimMask, aimTriggerInteraction);
            foreach (RaycastHit hit in hits.OrderBy(candidate => candidate.distance))
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                aimPoint = hit.point;
                break;
            }

            Vector3 direction = aimPoint - spawnPosition;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : castOrigin.forward;
        }

        private SpellDefinition GetCurrentSpell()
        {
            return loadout == null ? primarySpell : loadout.SelectedSpell;
        }

        private void HandleProjectileCompleted(SpellProjectile projectile)
        {
            if (projectile != null)
            {
                projectile.Completed -= HandleProjectileCompleted;
                activeProjectiles.Remove(projectile);
            }
        }

        private static void ReportFailure(string message)
        {
            Debug.Log(message);

            if (message == "Not enough mana." ||
                message == "No spell assigned." ||
                message.Contains("cooldown"))
            {
                PrototypeHudMessageBus.Show(message);
            }
        }
    }
}
