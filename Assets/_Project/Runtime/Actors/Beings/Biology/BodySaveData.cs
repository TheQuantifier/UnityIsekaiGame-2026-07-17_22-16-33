using System;

namespace UnityIsekaiGame.Beings.Biology
{
    [Serializable]
    public sealed class BodySaveData
    {
        public const int CurrentSchemaVersion = 1;
        public int schemaVersion = CurrentSchemaVersion;
        public string actorBodyId;
        public string personId;
        public string speciesDefinitionId;
        public long bodyRevision;
    }
}
