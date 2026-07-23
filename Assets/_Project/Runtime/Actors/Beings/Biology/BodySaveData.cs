using System;
using UnityIsekaiGame.Beings.Biology.Anatomy;
using UnityIsekaiGame.Beings.Biology.Condition;
using UnityIsekaiGame.Beings.Biology.Hazards;
using UnityIsekaiGame.Beings.Biology.Recovery;
using UnityIsekaiGame.Beings.Biology.VitalProcesses;

namespace UnityIsekaiGame.Beings.Biology
{
    [Serializable]
    public sealed class BodySaveData
    {
        public const int CurrentSchemaVersion = 6;
        public int schemaVersion = CurrentSchemaVersion;
        public string actorBodyId;
        public string personId;
        public string speciesDefinitionId;
        public long bodyRevision;
        public AnatomySaveData anatomy;
        public BodyConditionSaveData condition;
        public VitalProcessSaveData vitalProcesses;
        public BiologicalHazardSaveData biologicalHazards;
        public BiologicalRecoverySaveData biologicalRecovery;
    }
}
