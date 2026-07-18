using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        [SerializeField] private SpellDefinition primarySpell;
        [SerializeField] private LayerMask aimMask = ~0;
        [SerializeField] private QueryTriggerInteraction aimTriggerInteraction = QueryTriggerInteraction.Ignore;

        private readonly List<SpellProjectile> activeProjectiles = new List<SpellProjectile>();
        private float nextCastTime;

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
            SpellCastResult validation = ValidateCast();
            if (!validation.Succeeded)
            {
                ReportFailure(validation.Message);
                return validation;
            }

            VitalChangeResult manaSpend = mana.Spend(primarySpell.ManaCost);
            if (!manaSpend.Succeeded)
            {
                ReportFailure(manaSpend.Message);
                return SpellCastResult.Failure(manaSpend.Message);
            }

            SpellProjectile projectile = SpawnProjectile(primarySpell);
            if (projectile == null)
            {
                mana.Restore(primarySpell.ManaCost);
                return SpellCastResult.Failure("Invalid projectile configuration.");
            }

            nextCastTime = Time.time + primarySpell.Cooldown;
            string message = $"Cast {primarySpell.DisplayName}.";
            Debug.Log(message);
            return SpellCastResult.Success(message);
        }

        public void ResetSpellcasting()
        {
            nextCastTime = 0f;
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

        private SpellCastResult ValidateCast()
        {
            if (primarySpell == null)
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

            if (Time.time < nextCastTime)
            {
                return SpellCastResult.Failure($"{primarySpell.DisplayName} is on cooldown.");
            }

            if (mana == null)
            {
                return SpellCastResult.Failure("No mana source assigned.");
            }

            if (!mana.CanSpend(primarySpell.ManaCost))
            {
                return SpellCastResult.Failure("Not enough mana.");
            }

            if (castOrigin == null || primarySpell.ProjectilePrefab == null)
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
