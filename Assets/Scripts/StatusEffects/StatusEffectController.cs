using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Stats;

namespace UnityIsekaiGame.StatusEffects
{
    public sealed class StatusEffectController : MonoBehaviour, IStatusEffectReceiver
    {
        private readonly List<RuntimeStatusEffect> activeStatuses = new List<RuntimeStatusEffect>();
        private IRuntimeStatReceiver statReceiver;

        public IReadOnlyList<RuntimeStatusEffect> ActiveStatuses => activeStatuses;
        public StatusEffectController StatusController => this;
        public event Action<RuntimeStatusEffect> StatusAdded;
        public event Action<RuntimeStatusEffect> StatusChanged;
        public event Action<RuntimeStatusEffect> StatusRemoved;
        public event Action<RuntimeStatusEffect> StatusExpired;

        private void Awake()
        {
            statReceiver = GetComponentInParent<IRuntimeStatReceiver>();
        }

        private void Update()
        {
            UpdateStatuses(Time.deltaTime);
        }

        public StatusApplicationResult CanApplyStatus(StatusEffectApplicationRequest request)
        {
            StatusApplicationResult validation = ValidateRequest(request);
            if (!validation.Succeeded)
            {
                return validation;
            }

            if (request.Definition.DurationModel == StatusDurationModel.Instant)
            {
                return StatusApplicationResult.Success(null, $"Can apply instant status {request.Definition.DisplayName}.");
            }

            RuntimeStatusEffect existing = FindFirstActive(request.Definition.Id);
            if (existing == null || request.Definition.StackingPolicy == StatusStackingPolicy.IndependentInstances)
            {
                return StatusApplicationResult.Success(existing, $"{request.Definition.DisplayName} can apply.");
            }

            return EvaluateExistingStatus(existing, request.Definition);
        }

        public StatusApplicationResult ApplyStatus(StatusEffectApplicationRequest request)
        {
            StatusApplicationResult validation = CanApplyStatus(request);
            if (!validation.Succeeded)
            {
                return validation;
            }

            if (request.Definition.DurationModel == StatusDurationModel.Instant)
            {
                return StatusApplicationResult.Success(null, $"Applied instant status {request.Definition.DisplayName}.");
            }

            RuntimeStatusEffect existing = FindFirstActive(request.Definition.Id);
            if (existing != null && request.Definition.StackingPolicy != StatusStackingPolicy.IndependentInstances)
            {
                return ApplyStackingPolicy(existing, request);
            }

            RuntimeStatusEffect created = CreateRuntimeStatus(request);
            if (!RegisterModifiers(created))
            {
                return StatusApplicationResult.Failure(StatusApplicationStatus.InvalidModifier, $"Could not register modifiers for {request.Definition.DisplayName}.");
            }

            activeStatuses.Add(created);
            StatusAdded?.Invoke(created);
            return StatusApplicationResult.Success(created, $"Applied {request.Definition.DisplayName}.");
        }

        public bool RemoveStatus(string applicationId)
        {
            RuntimeStatusEffect status = FindByApplicationId(applicationId);
            if (status == null || !status.Remove())
            {
                return false;
            }

            UnregisterModifiers(status);
            activeStatuses.Remove(status);
            StatusRemoved?.Invoke(status);
            return true;
        }

        public bool RemoveStatusesByDefinition(string definitionId)
        {
            bool removedAny = false;
            for (int i = activeStatuses.Count - 1; i >= 0; i--)
            {
                if (activeStatuses[i].Definition.Id == definitionId)
                {
                    removedAny |= RemoveStatus(activeStatuses[i].ApplicationId);
                }
            }

            return removedAny;
        }

        public bool RemoveStatusesBySource(string sourceId)
        {
            bool removedAny = false;
            for (int i = activeStatuses.Count - 1; i >= 0; i--)
            {
                if (string.Equals(activeStatuses[i].SourceId, sourceId, StringComparison.Ordinal))
                {
                    removedAny |= RemoveStatus(activeStatuses[i].ApplicationId);
                }
            }

            return removedAny;
        }

        public void ClearTemporaryStatuses()
        {
            for (int i = activeStatuses.Count - 1; i >= 0; i--)
            {
                RuntimeStatusEffect status = activeStatuses[i];
                if (status.Definition.DurationModel == StatusDurationModel.Timed || status.Definition.DurationModel == StatusDurationModel.Instant)
                {
                    RemoveStatus(status.ApplicationId);
                }
            }
        }

        public void ClearAllStatuses()
        {
            for (int i = activeStatuses.Count - 1; i >= 0; i--)
            {
                RemoveStatus(activeStatuses[i].ApplicationId);
            }
        }

        public void UpdateStatuses(float deltaTime)
        {
            for (int i = activeStatuses.Count - 1; i >= 0; i--)
            {
                RuntimeStatusEffect status = activeStatuses[i];
                if (!status.Advance(deltaTime))
                {
                    continue;
                }

                UnregisterModifiers(status);
                activeStatuses.RemoveAt(i);
                StatusExpired?.Invoke(status);
            }
        }

        public RuntimeStatusEffect FindByApplicationId(string applicationId)
        {
            for (int i = 0; i < activeStatuses.Count; i++)
            {
                if (string.Equals(activeStatuses[i].ApplicationId, applicationId, StringComparison.Ordinal))
                {
                    return activeStatuses[i];
                }
            }

            return null;
        }

        public RuntimeStatusEffect FindFirstActive(string definitionId)
        {
            for (int i = 0; i < activeStatuses.Count; i++)
            {
                RuntimeStatusEffect status = activeStatuses[i];
                if (status.IsActive && status.Definition.Id == definitionId)
                {
                    return status;
                }
            }

            return null;
        }

        public List<StatusEffectSaveData> CreateSaveData()
        {
            List<StatusEffectSaveData> saveData = new List<StatusEffectSaveData>(activeStatuses.Count);
            for (int i = 0; i < activeStatuses.Count; i++)
            {
                saveData.Add(activeStatuses[i].CreateSaveData());
            }

            return saveData;
        }

        public void RefreshStatusModifiers(RuntimeStatusEffect status)
        {
            if (status == null || !activeStatuses.Contains(status))
            {
                return;
            }

            RebuildModifiers(status);
            StatusChanged?.Invoke(status);
        }

        private StatusApplicationResult ValidateRequest(StatusEffectApplicationRequest request)
        {
            if (request.Definition == null)
            {
                return StatusApplicationResult.Failure(StatusApplicationStatus.MissingDefinition, "Missing status effect definition.");
            }

            float duration = request.Definition.ResolveDuration(request.DurationOverride);
            if (request.Definition.DurationModel == StatusDurationModel.Timed && duration <= 0f)
            {
                return StatusApplicationResult.Failure(StatusApplicationStatus.InvalidDuration, $"{request.Definition.DisplayName} has no positive duration.");
            }

            if (FindByApplicationId(request.ApplicationId) != null)
            {
                return StatusApplicationResult.Failure(StatusApplicationStatus.DuplicateApplicationId, $"Status application ID '{request.ApplicationId}' is already active.");
            }

            if (!CanReceiveModifiers(request.Definition))
            {
                return StatusApplicationResult.Failure(StatusApplicationStatus.TargetLacksStat, $"{name} lacks a required runtime stat receiver.");
            }

            return StatusApplicationResult.Success(null, "Status can apply.");
        }

        private StatusApplicationResult ApplyStackingPolicy(RuntimeStatusEffect existing, StatusEffectApplicationRequest request)
        {
            switch (request.Definition.StackingPolicy)
            {
                case StatusStackingPolicy.RejectDuplicate:
                    return StatusApplicationResult.Failure(StatusApplicationStatus.DuplicateRejected, $"{request.Definition.DisplayName} is already active.");
                case StatusStackingPolicy.RefreshDuration:
                    Refresh(existing, request);
                    return StatusApplicationResult.Success(existing, $"Refreshed {request.Definition.DisplayName}.");
                case StatusStackingPolicy.ReplaceExisting:
                    RemoveStatus(existing.ApplicationId);
                    return ApplyStatus(CreateReplacementRequest(request));
                case StatusStackingPolicy.AddStack:
                    if (!existing.AddStack())
                    {
                        return StatusApplicationResult.Failure(StatusApplicationStatus.MaximumStacksReached, $"{request.Definition.DisplayName} is already at maximum stacks.");
                    }

                    RebuildModifiers(existing);
                    Refresh(existing, request);
                    StatusChanged?.Invoke(existing);
                    return StatusApplicationResult.Success(existing, $"Added a stack of {request.Definition.DisplayName}.");
                default:
                    return StatusApplicationResult.Failure(StatusApplicationStatus.DuplicateRejected, $"{request.Definition.DisplayName} is already active.");
            }
        }

        private StatusApplicationResult EvaluateExistingStatus(RuntimeStatusEffect existing, StatusEffectDefinition definition)
        {
            switch (definition.StackingPolicy)
            {
                case StatusStackingPolicy.RejectDuplicate:
                    return StatusApplicationResult.Failure(StatusApplicationStatus.DuplicateRejected, $"{definition.DisplayName} is already active.");
                case StatusStackingPolicy.AddStack:
                    return existing.StackCount >= definition.MaximumStacks
                        ? StatusApplicationResult.Failure(StatusApplicationStatus.MaximumStacksReached, $"{definition.DisplayName} is already at maximum stacks.")
                        : StatusApplicationResult.Success(existing, $"{definition.DisplayName} can add a stack.");
                case StatusStackingPolicy.RefreshDuration:
                case StatusStackingPolicy.ReplaceExisting:
                    return StatusApplicationResult.Success(existing, $"{definition.DisplayName} can update an existing status.");
                default:
                    return StatusApplicationResult.Failure(StatusApplicationStatus.DuplicateRejected, $"{definition.DisplayName} is already active.");
            }
        }

        private RuntimeStatusEffect CreateRuntimeStatus(StatusEffectApplicationRequest request)
        {
            float duration = request.Definition.DurationModel == StatusDurationModel.Timed
                ? request.Definition.ResolveDuration(request.DurationOverride)
                : 0f;
            string applicationId = string.IsNullOrWhiteSpace(request.ApplicationId) ? Guid.NewGuid().ToString("D") : request.ApplicationId;
            string sourceId = string.IsNullOrWhiteSpace(request.SourceId) ? applicationId : request.SourceId;
            return new RuntimeStatusEffect(request.Definition, applicationId, sourceId, request.Source, gameObject, duration, request.Now);
        }

        private static StatusEffectApplicationRequest CreateReplacementRequest(StatusEffectApplicationRequest request)
        {
            return new StatusEffectApplicationRequest(request.Definition, request.Source, request.SourceId, request.DurationOverride, string.Empty, request.Now);
        }

        private void Refresh(RuntimeStatusEffect status, StatusEffectApplicationRequest request)
        {
            if (request.Definition.RefreshPolicy == StatusRefreshPolicy.None)
            {
                return;
            }

            float duration = request.Definition.ResolveDuration(request.DurationOverride);
            if (request.Definition.RefreshPolicy == StatusRefreshPolicy.ExtendByDefaultDuration)
            {
                duration = status.RemainingDuration + duration;
            }

            status.Refresh(duration);
            StatusChanged?.Invoke(status);
        }

        private bool CanReceiveModifiers(StatusEffectDefinition definition)
        {
            if (definition.StatModifiers.Count == 0)
            {
                return true;
            }

            statReceiver ??= GetComponentInParent<IRuntimeStatReceiver>();
            if (statReceiver == null)
            {
                return false;
            }

            for (int i = 0; i < definition.StatModifiers.Count; i++)
            {
                StatModifierDefinition modifier = definition.StatModifiers[i];
                if (modifier == null || !modifier.IsValid || !statReceiver.HasStat(modifier.StatType))
                {
                    return false;
                }
            }

            return true;
        }

        private bool RegisterModifiers(RuntimeStatusEffect status)
        {
            if (status.Definition.StatModifiers.Count == 0)
            {
                return true;
            }

            statReceiver ??= GetComponentInParent<IRuntimeStatReceiver>();
            if (statReceiver == null)
            {
                return false;
            }

            for (int i = 0; i < status.Definition.StatModifiers.Count; i++)
            {
                RuntimeStatModifier modifier = status.Definition.StatModifiers[i].CreateRuntimeModifier(status.ModifierSource, status.StackCount);
                if (!statReceiver.AddModifier(modifier))
                {
                    statReceiver.RemoveModifiersFromSource(status.ModifierSource);
                    return false;
                }
            }

            return true;
        }

        private void UnregisterModifiers(RuntimeStatusEffect status)
        {
            statReceiver ??= GetComponentInParent<IRuntimeStatReceiver>();
            statReceiver?.RemoveModifiersFromSource(status.ModifierSource);
        }

        private void RebuildModifiers(RuntimeStatusEffect status)
        {
            UnregisterModifiers(status);
            RegisterModifiers(status);
        }
    }
}
