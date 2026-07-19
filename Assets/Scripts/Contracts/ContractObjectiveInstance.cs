using System;
using UnityIsekaiGame.Persistence;

namespace UnityIsekaiGame.Contracts
{
    public abstract class ContractObjectiveInstance : IDisposable
    {
        private bool completed;

        protected ContractObjectiveInstance(ContractObjectiveDefinition definition)
        {
            Definition = definition;
        }

        public ContractObjectiveDefinition Definition { get; }
        public string Description => Definition == null ? "Missing objective" : Definition.Description;
        public abstract int CurrentProgress { get; }
        public abstract int RequiredProgress { get; }
        public bool IsComplete => completed;

        public event Action<ContractObjectiveInstance> ProgressChanged;
        public event Action<ContractObjectiveInstance> Completed;

        public virtual void Activate()
        {
            RefreshProgress();
        }

        public virtual void ActivateForRestore()
        {
        }

        public virtual void Deactivate()
        {
        }

        public abstract void RefreshProgress();

        public virtual ObjectiveProgressSaveData CreateSaveData(int objectiveIndex, string objectiveKey)
        {
            return new ObjectiveProgressSaveData
            {
                objectiveKey = objectiveKey,
                objectiveId = Definition == null ? string.Empty : Definition.ObjectiveId,
                objectiveIndex = objectiveIndex,
                objectiveType = Definition == null ? string.Empty : Definition.GetType().Name,
                currentProgress = CurrentProgress,
                requiredProgress = RequiredProgress,
                completed = IsComplete
            };
        }

        public virtual bool TryRestoreFromSaveData(ObjectiveProgressSaveData saveData, out string failureReason)
        {
            failureReason = string.Empty;
            if (!ValidateCommonSaveData(saveData, out failureReason))
            {
                return false;
            }

            RestoreCompleted(saveData.completed);
            return true;
        }

        public virtual void Dispose()
        {
            Deactivate();
        }

        protected bool ValidateCommonSaveData(ObjectiveProgressSaveData saveData, out string failureReason)
        {
            failureReason = string.Empty;
            if (saveData == null)
            {
                failureReason = "Objective save data is missing.";
                return false;
            }

            string expectedType = Definition == null ? string.Empty : Definition.GetType().Name;
            if (!string.Equals(saveData.objectiveType, expectedType, StringComparison.Ordinal))
            {
                failureReason = $"Objective '{saveData.objectiveKey}' expected type '{expectedType}' but save has '{saveData.objectiveType}'.";
                return false;
            }

            if (saveData.currentProgress < 0 || saveData.requiredProgress != RequiredProgress || saveData.currentProgress > RequiredProgress)
            {
                failureReason = $"Objective '{saveData.objectiveKey}' has invalid progress {saveData.currentProgress}/{saveData.requiredProgress}.";
                return false;
            }

            if (saveData.completed && saveData.currentProgress < RequiredProgress)
            {
                failureReason = $"Objective '{saveData.objectiveKey}' is marked complete below required progress.";
                return false;
            }

            return true;
        }

        protected void RestoreCompleted(bool value)
        {
            completed = value;
        }

        protected void NotifyProgressChanged()
        {
            bool completedNow = !completed && CurrentProgress >= RequiredProgress;
            if (completedNow)
            {
                completed = true;
            }

            ProgressChanged?.Invoke(this);

            if (completedNow)
            {
                Completed?.Invoke(this);
            }
        }
    }
}
