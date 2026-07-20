using System;
using System.Collections.Generic;
using UnityEngine;
using UnityIsekaiGame.Contracts;

namespace UnityIsekaiGame.Quests
{
    [Serializable]
    public sealed class QuestStageDefinition
    {
        [SerializeField] private string stageId;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private ContractObjectiveDefinition[] objectives;
        [SerializeField] private int nextStageIndex = -1;

        public string StageId => stageId;
        public string Description => description;
        public IReadOnlyList<ContractObjectiveDefinition> Objectives => objectives ?? Array.Empty<ContractObjectiveDefinition>();
        public int NextStageIndex => nextStageIndex;
    }
}
