using System;
using UnityEngine;
using UnityIsekaiGame.Capabilities;
using UnityIsekaiGame.CharacterSystem;
using UnityIsekaiGame.ResourceSystem;
using UnityIsekaiGame.Stats;
using UnityIsekaiGame.Traits;
using UnityIsekaiGame.WorldEntities;

namespace UnityIsekaiGame.Combat
{
    public sealed class DamageHealingService
    {
        public event Action<DamageApplicationResult> DamageResolved;
        public event Action<DamageApplicationResult> DamagePrevented;
        public event Action<HealingApplicationResult> HealingResolved;

        public DamageApplicationResult PreviewDamage(DamageApplicationRequest request)
        {
            return EvaluateDamage(request, execute: false);
        }

        public DamageApplicationResult ApplyDamage(DamageApplicationRequest request)
        {
            return EvaluateDamage(request, execute: true);
        }

        public HealingApplicationResult PreviewHealing(HealingApplicationRequest request)
        {
            return EvaluateHealing(request, execute: false);
        }

        public HealingApplicationResult ApplyHealing(HealingApplicationRequest request)
        {
            return EvaluateHealing(request, execute: true);
        }

        private DamageApplicationResult EvaluateDamage(DamageApplicationRequest request, bool execute)
        {
            if (!IsFinite(request.RequestedAmount) || request.RequestedAmount < 0f)
            {
                return DamageApplicationResult.Failure(request, ImmediateCombatResultCode.InvalidRequest, "Damage amount must be finite and non-negative.");
            }

            if (request.DamageType == null)
            {
                return DamageApplicationResult.Failure(request, ImmediateCombatResultCode.InvalidRequest, "Damage type is missing.");
            }

            if (!TryResolveTarget(request.TargetObject, request.TargetActorId, out TargetRuntime target, out string failureCode, out string failureMessage))
            {
                return DamageApplicationResult.Failure(request, failureCode, failureMessage);
            }

            float requested = Mathf.Max(0f, request.RequestedAmount);
            bool trueDamage = request.DamageType.IsTrueDamage;
            bool immune = !trueDamage && IsImmune(target, request.DamageType);
            float defense = !trueDamage
                ? Mathf.Max(0f, ResolveDefense(target, request.DamageType))
                : 0f;
            float afterDefense = immune ? 0f : Mathf.Max(0f, requested - defense);
            float defenseMitigation = immune ? 0f : Mathf.Max(0f, requested - afterDefense);
            float resistance = !trueDamage && !immune ? ResolveResistance(target, request.DamageType) : 0f;
            float afterResistance = afterDefense * (1f - resistance);
            float resistanceMitigation = Mathf.Max(0f, afterDefense - afterResistance);
            float finalDamage = immune ? 0f : Mathf.Max(0f, afterResistance);
            float oldHealth = target.Health.Current;
            float previewNewHealth = Mathf.Max(target.Health.Minimum, oldHealth - finalDamage);
            float overkill = Mathf.Max(0f, finalDamage - Mathf.Max(0f, oldHealth - target.Health.Minimum));
            bool wouldChange = finalDamage > CharacterResourceCollection.Epsilon && !Mathf.Approximately(oldHealth, previewNewHealth);
            bool wouldBecomeZero = oldHealth > target.Health.Minimum + CharacterResourceCollection.Epsilon && previewNewHealth <= target.Health.Minimum + CharacterResourceCollection.Epsilon;

            if (!execute)
            {
                return DamageApplicationResult.Create(true, ImmediateCombatResultCode.Preview, "Damage preview calculated without mutating Health.", request, target.ActorId, requested, defense, defenseMitigation, resistance, resistanceMitigation, finalDamage, oldHealth, previewNewHealth, target.Health.Minimum, target.Health.Maximum, immune, trueDamage, false, wouldChange, wouldBecomeZero, overkill, null);
            }

            ResourceChangeResult resourceResult = null;
            float newHealth = oldHealth;
            bool healthChanged = false;
            bool becameZero = false;
            string code = immune || finalDamage <= CharacterResourceCollection.Epsilon ? ImmediateCombatResultCode.Prevented : ImmediateCombatResultCode.Applied;
            string message = immune ? $"{request.DamageType.DisplayName} damage was prevented by immunity." : finalDamage <= CharacterResourceCollection.Epsilon ? $"{request.DamageType.DisplayName} damage was fully mitigated." : $"{request.DamageType.DisplayName} damage applied.";

            if (finalDamage > CharacterResourceCollection.Epsilon)
            {
                resourceResult = target.Resources.ApplyChange(new ResourceChangeRequest(
                    ResourceIds.Health,
                    ResourceChangeOperation.Damage,
                    finalDamage,
                    ResourceChangeSourceCategory.Combat,
                    request.SourceActorId,
                    request.Reason,
                    request.TransactionId,
                    allowPartial: true));
                if (!resourceResult.Succeeded)
                {
                    return DamageApplicationResult.Failure(request, ImmediateCombatResultCode.ResourceRejected, resourceResult.Message, target.ActorId);
                }

                newHealth = resourceResult.NewCurrent;
                healthChanged = resourceResult.AppliedAmount > CharacterResourceCollection.Epsilon;
                becameZero = resourceResult.BecameEmpty;
                code = resourceResult.DuplicateEvent ? ImmediateCombatResultCode.DuplicateTransaction : resourceResult.AppliedAmount > 0f ? ImmediateCombatResultCode.Applied : ImmediateCombatResultCode.NoChange;
                message = resourceResult.Message;
            }

            DamageApplicationResult result = DamageApplicationResult.Create(false, code, message, request, target.ActorId, requested, defense, defenseMitigation, resistance, resistanceMitigation, finalDamage, oldHealth, newHealth, target.Health.Minimum, target.Health.Maximum, immune, trueDamage, resourceResult?.DuplicateEvent ?? false, healthChanged, becameZero, overkill, resourceResult);
            if (immune || finalDamage <= CharacterResourceCollection.Epsilon)
            {
                DamagePrevented?.Invoke(result);
            }

            DamageResolved?.Invoke(result);
            return result;
        }

        private HealingApplicationResult EvaluateHealing(HealingApplicationRequest request, bool execute)
        {
            if (!IsFinite(request.RequestedAmount) || request.RequestedAmount < 0f)
            {
                return HealingApplicationResult.Failure(request, ImmediateCombatResultCode.InvalidRequest, "Healing amount must be finite and non-negative.");
            }

            if (!TryResolveTarget(request.TargetObject, request.TargetActorId, out TargetRuntime target, out string failureCode, out string failureMessage))
            {
                return HealingApplicationResult.Failure(request, failureCode, failureMessage);
            }

            float requested = Mathf.Max(0f, request.RequestedAmount);
            float oldHealth = target.Health.Current;
            float missing = Mathf.Max(0f, target.Health.Maximum - oldHealth);
            float finalHealing = Mathf.Min(requested, missing);
            float overheal = Mathf.Max(0f, requested - finalHealing);
            float previewNewHealth = Mathf.Min(target.Health.Maximum, oldHealth + finalHealing);
            bool wouldChange = finalHealing > CharacterResourceCollection.Epsilon && !Mathf.Approximately(oldHealth, previewNewHealth);
            bool wouldBecomeFull = oldHealth < target.Health.Maximum - CharacterResourceCollection.Epsilon && previewNewHealth >= target.Health.Maximum - CharacterResourceCollection.Epsilon;

            if (!execute)
            {
                return HealingApplicationResult.Create(true, ImmediateCombatResultCode.Preview, "Healing preview calculated without mutating Health.", request, target.ActorId, requested, finalHealing, overheal, oldHealth, previewNewHealth, target.Health.Minimum, target.Health.Maximum, false, wouldChange, wouldBecomeFull, null);
            }

            ResourceChangeResult resourceResult = null;
            float newHealth = oldHealth;
            bool healthChanged = false;
            bool becameFull = false;
            string code = finalHealing <= CharacterResourceCollection.Epsilon ? ImmediateCombatResultCode.NoChange : ImmediateCombatResultCode.Healed;
            string message = finalHealing <= CharacterResourceCollection.Epsilon ? "Health is already full." : "Healing applied.";

            if (finalHealing > CharacterResourceCollection.Epsilon)
            {
                resourceResult = target.Resources.ApplyHealing(ResourceIds.Health, finalHealing, request.SourceActorId, request.Reason, request.TransactionId);
                if (!resourceResult.Succeeded)
                {
                    return HealingApplicationResult.Failure(request, ImmediateCombatResultCode.ResourceRejected, resourceResult.Message, target.ActorId);
                }

                newHealth = resourceResult.NewCurrent;
                healthChanged = resourceResult.AppliedAmount > CharacterResourceCollection.Epsilon;
                becameFull = resourceResult.BecameFull;
                code = resourceResult.DuplicateEvent ? ImmediateCombatResultCode.DuplicateTransaction : code;
                message = resourceResult.Message;
            }

            HealingApplicationResult result = HealingApplicationResult.Create(false, code, message, request, target.ActorId, requested, finalHealing, overheal, oldHealth, newHealth, target.Health.Minimum, target.Health.Maximum, resourceResult?.DuplicateEvent ?? false, healthChanged, becameFull, resourceResult);
            HealingResolved?.Invoke(result);
            return result;
        }

        private static bool TryResolveTarget(GameObject targetObject, string requestedTargetActorId, out TargetRuntime target, out string failureCode, out string failureMessage)
        {
            target = default;
            failureCode = string.Empty;
            failureMessage = string.Empty;
            if (targetObject == null)
            {
                failureCode = ImmediateCombatResultCode.MissingTarget;
                failureMessage = "Target object is missing.";
                return false;
            }

            CharacterSystemCoordinator character = targetObject.GetComponentInParent<CharacterSystemCoordinator>();
            CharacterResourceCollection resources = character == null ? targetObject.GetComponentInParent<CharacterResourceCollection>() : character.Resources;
            if (resources == null || !resources.TryGetResource(ResourceIds.Health, out ResourceSnapshot health))
            {
                failureCode = ImmediateCombatResultCode.MissingHealth;
                failureMessage = "Target does not expose the Health resource.";
                return false;
            }

            string resolvedActorId = ResolveActorId(targetObject, character);
            if (string.IsNullOrWhiteSpace(resolvedActorId))
            {
                failureCode = ImmediateCombatResultCode.MissingTarget;
                failureMessage = "Target actor identity is missing.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(requestedTargetActorId) && !string.Equals(requestedTargetActorId, resolvedActorId, StringComparison.Ordinal))
            {
                failureCode = ImmediateCombatResultCode.StaleTarget;
                failureMessage = $"Target actor identity '{requestedTargetActorId}' no longer resolves to '{resolvedActorId}'.";
                return false;
            }

            target = new TargetRuntime(
                targetObject,
                resolvedActorId,
                resources,
                character == null ? targetObject.GetComponentInParent<CalculatedStatCollection>() : character.CalculatedStats,
                character == null ? targetObject.GetComponentInParent<CharacterTraitCollection>() : character.Traits,
                health);
            return true;
        }

        private static string ResolveActorId(GameObject targetObject, CharacterSystemCoordinator character)
        {
            if (character != null && !string.IsNullOrWhiteSpace(character.ActorId))
            {
                return character.ActorId;
            }

            WorldEntityIdentity worldEntity = targetObject.GetComponentInParent<WorldEntityIdentity>();
            return worldEntity == null ? string.Empty : worldEntity.EntityId;
        }

        private static float ResolveDefense(TargetRuntime target, DamageTypeDefinition damageType)
        {
            if (target.Stats == null)
            {
                return 0f;
            }

            string statId = damageType != null && damageType.Family == DamageFamily.Physical
                ? CalculatedStatIds.PhysicalDefense
                : CalculatedStatIds.MagicalDefense;
            return target.Stats.HasStat(statId) ? target.Stats.GetValue(statId) : 0f;
        }

        private static bool IsImmune(TargetRuntime target, DamageTypeDefinition damageType)
        {
            if (target.Traits == null || damageType == null)
            {
                return false;
            }

            CapabilitySnapshot capability = target.Traits.Capabilities.Evaluate(damageType.ImmunityCapabilityId);
            return capability.BooleanValue || target.Traits.IsImmuneTo(damageType.Id);
        }

        private static float ResolveResistance(TargetRuntime target, DamageTypeDefinition damageType)
        {
            if (target.Traits == null || damageType == null)
            {
                return 0f;
            }

            CapabilitySnapshot capability = target.Traits.Capabilities.Evaluate(damageType.ResistanceCapabilityId);
            float traitResistance = target.Traits.GetResistance(damageType.Id);
            return Mathf.Clamp01(capability.NumericValue + traitResistance);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private readonly struct TargetRuntime
        {
            public TargetRuntime(GameObject gameObject, string actorId, CharacterResourceCollection resources, CalculatedStatCollection stats, CharacterTraitCollection traits, ResourceSnapshot health)
            {
                GameObject = gameObject;
                ActorId = actorId ?? string.Empty;
                Resources = resources;
                Stats = stats;
                Traits = traits;
                Health = health;
            }

            public GameObject GameObject { get; }
            public string ActorId { get; }
            public CharacterResourceCollection Resources { get; }
            public CalculatedStatCollection Stats { get; }
            public CharacterTraitCollection Traits { get; }
            public ResourceSnapshot Health { get; }
        }
    }
}
