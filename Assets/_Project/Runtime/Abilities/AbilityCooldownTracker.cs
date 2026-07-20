using System.Collections.Generic;
using UnityEngine;

namespace UnityIsekaiGame.Abilities
{
    public sealed class AbilityCooldownTracker
    {
        private readonly Dictionary<string, float> cooldownEndsByAbilityId = new Dictionary<string, float>();

        public bool IsOnCooldown(AbilityDefinition ability, float now, out float remainingSeconds)
        {
            remainingSeconds = 0f;
            if (ability == null || string.IsNullOrWhiteSpace(ability.Id))
            {
                return false;
            }

            if (!cooldownEndsByAbilityId.TryGetValue(ability.Id, out float endTime) || now >= endTime)
            {
                return false;
            }

            remainingSeconds = Mathf.Max(0f, endTime - now);
            return remainingSeconds > 0f;
        }

        public void StartCooldown(AbilityDefinition ability, float now)
        {
            if (ability == null || string.IsNullOrWhiteSpace(ability.Id) || ability.CooldownDuration <= 0f)
            {
                return;
            }

            cooldownEndsByAbilityId[ability.Id] = now + ability.CooldownDuration;
        }

        public float GetRemaining(AbilityDefinition ability, float now)
        {
            return IsOnCooldown(ability, now, out float remaining) ? remaining : 0f;
        }

        public void Clear(AbilityDefinition ability)
        {
            if (ability != null && !string.IsNullOrWhiteSpace(ability.Id))
            {
                cooldownEndsByAbilityId.Remove(ability.Id);
            }
        }

        public void Reset()
        {
            cooldownEndsByAbilityId.Clear();
        }
    }
}
