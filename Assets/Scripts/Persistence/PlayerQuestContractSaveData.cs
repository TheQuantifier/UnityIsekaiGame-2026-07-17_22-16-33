using System;
using System.Collections.Generic;
using UnityIsekaiGame.Contracts;
using UnityIsekaiGame.Quests;

namespace UnityIsekaiGame.Persistence
{
    [Serializable]
    public sealed class PlayerQuestContractSaveData
    {
        public int schemaVersion = PlayerQuestContractPersistenceParticipant.CurrentParticipantSchemaVersion;
        public List<QuestInstanceSaveData> quests = new List<QuestInstanceSaveData>();
        public List<ContractInstanceSaveData> contracts = new List<ContractInstanceSaveData>();
    }

    [Serializable]
    public sealed class QuestInstanceSaveData
    {
        public string questDefinitionId;
        public string runtimeInstanceId;
        public QuestState state;
        public int currentStageIndex;
        public string currentStageId;
        public List<ObjectiveProgressSaveData> objectives = new List<ObjectiveProgressSaveData>();
    }

    [Serializable]
    public sealed class ContractInstanceSaveData
    {
        public string contractDefinitionId;
        public string runtimeInstanceId;
        public ContractState state;
        public List<ObjectiveProgressSaveData> objectives = new List<ObjectiveProgressSaveData>();
    }

    [Serializable]
    public sealed class ObjectiveProgressSaveData
    {
        public string objectiveKey;
        public string objectiveId;
        public int objectiveIndex;
        public string objectiveType;
        public int currentProgress;
        public int requiredProgress;
        public bool completed;
    }
}
