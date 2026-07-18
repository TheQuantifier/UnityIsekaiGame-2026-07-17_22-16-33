using System;

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

        public virtual void Deactivate()
        {
        }

        public abstract void RefreshProgress();

        public virtual void Dispose()
        {
            Deactivate();
        }

        protected void NotifyProgressChanged()
        {
            ProgressChanged?.Invoke(this);

            if (!completed && CurrentProgress >= RequiredProgress)
            {
                completed = true;
                Completed?.Invoke(this);
            }
        }
    }
}
