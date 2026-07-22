using System;
using UnityIsekaiGame.Beings.Biology.Anatomy;
using UnityIsekaiGame.Beings.Biology.Condition;

namespace UnityIsekaiGame.Beings.Biology
{
    [Serializable]
    public sealed class BodySaveData
    {
        public const int CurrentSchemaVersion = 3;
        public int schemaVersion = CurrentSchemaVersion;
        public string actorBodyId;
        public string personId;
        public string speciesDefinitionId;
        public long bodyRevision;
        public AnatomySaveData anatomy;
        public BodyConditionSaveData condition;
    }
}
