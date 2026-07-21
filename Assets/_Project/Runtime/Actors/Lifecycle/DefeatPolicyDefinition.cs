using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.GameData;
using UnityIsekaiGame.Requirements;

namespace UnityIsekaiGame.ActorLifecycle
{
    [CreateAssetMenu(fileName = "DefeatPolicyDefinition", menuName = "Unity Isekai Game/Actors/Defeat Policy")]
    public sealed class DefeatPolicyDefinition : ScriptableObject, IGameDefinition, IDefinitionCatalogValidationParticipant
    {
        [SerializeField] private string policyId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private DefeatPolicyOutcome zeroHealthOutcome = DefeatPolicyOutcome.BecomeUnconscious;
        [SerializeField] private bool allowUnconsciousness = true;
        [SerializeField] private bool allowDeath = true;
        [SerializeField] private bool allowRecovery = true;
        [SerializeField] private bool allowRevival = true;
        [SerializeField, Min(0f)] private float recoveryMinimumHealth = 1f;
        [SerializeField, Min(0f)] private float revivalMinimumHealth = 1f;
        [SerializeField] private RequirementSetDefinition recoveryRequirements;
        [SerializeField] private RequirementSetDefinition revivalRequirements;
        [SerializeField] private RequirementSetDefinition deathRequirements;
        [SerializeField] private string canBecomeUnconsciousCapabilityId = ActorLifecycleCapabilityIds.CanBecomeUnconscious;
        [SerializeField] private string canDieCapabilityId = ActorLifecycleCapabilityIds.CanDie;
        [SerializeField] private string canRecoverCapabilityId = ActorLifecycleCapabilityIds.CanRecover;
        [SerializeField] private string canBeRevivedCapabilityId = ActorLifecycleCapabilityIds.CanBeRevived;
        [SerializeField] private string deathImmunityCapabilityId = ActorLifecycleCapabilityIds.DeathImmunity;
        [SerializeField] private bool alphaEnabled = true;
        [SerializeField, TextArea] private string futureMetadata;

        public string Id => policyId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description ?? string.Empty;
        public DefeatPolicyOutcome ZeroHealthOutcome => zeroHealthOutcome;
        public bool AllowUnconsciousness => allowUnconsciousness;
        public bool AllowDeath => allowDeath;
        public bool AllowRecovery => allowRecovery;
        public bool AllowRevival => allowRevival;
        public float RecoveryMinimumHealth => Mathf.Max(0f, recoveryMinimumHealth);
        public float RevivalMinimumHealth => Mathf.Max(0f, revivalMinimumHealth);
        public RequirementSetDefinition RecoveryRequirements => recoveryRequirements;
        public RequirementSetDefinition RevivalRequirements => revivalRequirements;
        public RequirementSetDefinition DeathRequirements => deathRequirements;
        public string CanBecomeUnconsciousCapabilityId => string.IsNullOrWhiteSpace(canBecomeUnconsciousCapabilityId) ? ActorLifecycleCapabilityIds.CanBecomeUnconscious : canBecomeUnconsciousCapabilityId;
        public string CanDieCapabilityId => string.IsNullOrWhiteSpace(canDieCapabilityId) ? ActorLifecycleCapabilityIds.CanDie : canDieCapabilityId;
        public string CanRecoverCapabilityId => string.IsNullOrWhiteSpace(canRecoverCapabilityId) ? ActorLifecycleCapabilityIds.CanRecover : canRecoverCapabilityId;
        public string CanBeRevivedCapabilityId => string.IsNullOrWhiteSpace(canBeRevivedCapabilityId) ? ActorLifecycleCapabilityIds.CanBeRevived : canBeRevivedCapabilityId;
        public string DeathImmunityCapabilityId => string.IsNullOrWhiteSpace(deathImmunityCapabilityId) ? ActorLifecycleCapabilityIds.DeathImmunity : deathImmunityCapabilityId;
        public bool AlphaEnabled => alphaEnabled;
        public string FutureMetadata => futureMetadata ?? string.Empty;

        private void OnValidate()
        {
            policyId = policyId?.Trim();
            recoveryMinimumHealth = Mathf.Max(0f, recoveryMinimumHealth);
            revivalMinimumHealth = Mathf.Max(0f, revivalMinimumHealth);
            canBecomeUnconsciousCapabilityId = NormalizeCapabilityKey(canBecomeUnconsciousCapabilityId, ActorLifecycleCapabilityIds.CanBecomeUnconscious);
            canDieCapabilityId = NormalizeCapabilityKey(canDieCapabilityId, ActorLifecycleCapabilityIds.CanDie);
            canRecoverCapabilityId = NormalizeCapabilityKey(canRecoverCapabilityId, ActorLifecycleCapabilityIds.CanRecover);
            canBeRevivedCapabilityId = NormalizeCapabilityKey(canBeRevivedCapabilityId, ActorLifecycleCapabilityIds.CanBeRevived);
            deathImmunityCapabilityId = NormalizeCapabilityKey(deathImmunityCapabilityId, ActorLifecycleCapabilityIds.DeathImmunity);
        }

        public void ValidateCatalogDefinition(IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Id))
            {
                report.AddError($"DefeatPolicy '{name}' is missing a stable ID.");
            }
            else if (!Id.StartsWith("defeat-policy.", StringComparison.Ordinal))
            {
                report.AddWarning($"DefeatPolicy '{Id}' should use the 'defeat-policy.' namespace prefix.");
            }

            if (!Enum.IsDefined(typeof(DefeatPolicyOutcome), zeroHealthOutcome))
            {
                report.AddError($"DefeatPolicy '{DisplayName}' has an invalid zero-health outcome.");
            }

            if (zeroHealthOutcome == DefeatPolicyOutcome.BecomeUnconscious && !allowUnconsciousness)
            {
                report.AddError($"DefeatPolicy '{DisplayName}' cannot become unconscious while unconsciousness is disallowed.");
            }

            if (zeroHealthOutcome == DefeatPolicyOutcome.DieImmediately && !allowDeath)
            {
                report.AddError($"DefeatPolicy '{DisplayName}' cannot die immediately while death is disallowed.");
            }

            if (allowRecovery && recoveryMinimumHealth <= 0f)
            {
                report.AddWarning($"DefeatPolicy '{DisplayName}' allows recovery but restores no Health by default.");
            }

            if (allowRevival && revivalMinimumHealth <= 0f)
            {
                report.AddWarning($"DefeatPolicy '{DisplayName}' allows revival but restores no Health by default.");
            }

            ValidateCapabilityKey(CanBecomeUnconsciousCapabilityId, "become unconscious", report);
            ValidateCapabilityKey(CanDieCapabilityId, "die", report);
            ValidateCapabilityKey(CanRecoverCapabilityId, "recover", report);
            ValidateCapabilityKey(CanBeRevivedCapabilityId, "be revived", report);
            ValidateCapabilityKey(DeathImmunityCapabilityId, "death immunity", report);
            ValidateRequirementReference(recoveryRequirements, "recovery", definitionsById, report);
            ValidateRequirementReference(revivalRequirements, "revival", definitionsById, report);
            ValidateRequirementReference(deathRequirements, "death", definitionsById, report);
        }

        private static string NormalizeCapabilityKey(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static void ValidateCapabilityKey(string key, string label, DefinitionValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                report.AddError($"DefeatPolicy capability key for {label} is missing.");
                return;
            }

            if (!key.StartsWith("can.", StringComparison.Ordinal) && !key.StartsWith("immunity.", StringComparison.Ordinal))
            {
                report.AddWarning($"DefeatPolicy capability key '{key}' should use a lifecycle runtime namespace such as 'can.' or 'immunity.'.");
            }
        }

        private void ValidateRequirementReference(RequirementSetDefinition requirement, string label, IReadOnlyDictionary<string, IGameDefinition> definitionsById, DefinitionValidationReport report)
        {
            if (requirement == null)
            {
                return;
            }

            if (definitionsById == null || !definitionsById.TryGetValue(requirement.Id, out IGameDefinition found) || !ReferenceEquals(found, requirement))
            {
                report.AddError($"DefeatPolicy '{DisplayName}' {label} requirement '{requirement.Id}' is not registered in the catalog.");
            }
        }
    }
}
