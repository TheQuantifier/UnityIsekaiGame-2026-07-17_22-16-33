using System;
using System.Collections.Generic;
using UnityIsekaiGame.StatusEffects;

namespace UnityIsekaiGame.Persistence
{
    [Serializable]
    public sealed class PlayerStatsVitalsStatusSaveData
    {
        public int schemaVersion = PlayerStatsVitalsStatusPersistenceParticipant.CurrentParticipantSchemaVersion;
        public string actorProfileId;
        public int currentHealth;
        public float currentMana;
        public float currentStamina;
        public List<StatusEffectSaveData> statuses = new List<StatusEffectSaveData>();
    }
}
