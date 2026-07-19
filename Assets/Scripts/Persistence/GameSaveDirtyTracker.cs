using System;
using UnityEngine;

namespace UnityIsekaiGame.Persistence
{
    public sealed class GameSaveDirtyTracker : MonoBehaviour
    {
        [SerializeField] private bool isDirty;
        [SerializeField] private string lastReason;

        public event Action<bool, string> DirtyStateChanged;

        public bool IsDirty => isDirty;
        public string LastReason => lastReason ?? string.Empty;

        public void MarkDirty(string reason)
        {
            isDirty = true;
            lastReason = string.IsNullOrWhiteSpace(reason) ? "State changed." : reason;
            DirtyStateChanged?.Invoke(true, lastReason);
        }

        public void MarkClean(string reason)
        {
            isDirty = false;
            lastReason = string.IsNullOrWhiteSpace(reason) ? "State saved." : reason;
            DirtyStateChanged?.Invoke(false, lastReason);
        }

        public void DevelopmentSetDirty(bool dirty, string reason)
        {
            if (dirty)
            {
                MarkDirty(reason);
            }
            else
            {
                MarkClean(reason);
            }
        }
    }
}
