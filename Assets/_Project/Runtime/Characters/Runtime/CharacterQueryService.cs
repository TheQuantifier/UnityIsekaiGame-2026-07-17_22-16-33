using UnityIsekaiGame.Capabilities;
using UnityIsekaiGame.Requirements;
using UnityIsekaiGame.ResourceSystem;
using UnityIsekaiGame.Skills;
using UnityIsekaiGame.Traits;

namespace UnityIsekaiGame.CharacterSystem
{
    public sealed class CharacterQueryService
    {
        private readonly CharacterSystemCoordinator character;

        public CharacterQueryService(CharacterSystemCoordinator character)
        {
            this.character = character;
        }

        public bool IsReady => character != null && character.IsReady;
        public long Revision => character == null ? 0L : character.Revision;

        public float GetBaseAttribute(string attributeId)
        {
            return character?.Attributes == null ? 0f : character.Attributes.GetValue(attributeId);
        }

        public float GetCalculatedStat(string statId)
        {
            return character?.CalculatedStats == null ? 0f : character.CalculatedStats.GetValue(statId);
        }

        public ResourceSnapshot GetResource(string resourceId)
        {
            if (character?.Resources != null && character.Resources.TryGetResource(resourceId, out ResourceSnapshot snapshot))
            {
                return snapshot;
            }

            return default;
        }

        public SkillGrade GetSkillGrade(string skillId)
        {
            return character?.Skills == null ? SkillGrade.F : character.Skills.GetGrade(skillId);
        }

        public bool HasTrait(string traitId, bool includeDormant = false, bool includeSuppressed = false)
        {
            return character?.Traits != null && character.Traits.HasTrait(traitId, includeDormant, includeSuppressed);
        }

        public CapabilitySnapshot GetCapability(string capabilityId)
        {
            return character?.Traits == null ? null : character.Traits.Capabilities.Evaluate(capabilityId);
        }

        public RequirementEvaluationResult EvaluateRequirement(RequirementSetDefinition requirementSet)
        {
            if (!IsReady)
            {
                RequirementEvaluationResult notReady = new RequirementEvaluationResult
                {
                    Passed = false,
                    RequirementSetId = requirementSet == null ? string.Empty : requirementSet.Id
                };
                notReady.NodeResults.Add(new RequirementNodeResult
                {
                    Passed = false,
                    InternalReason = "Character System is not Ready.",
                    PlayerFacingReason = "Character is not ready.",
                    FailureVisibility = RequirementFailureVisibility.Visible
                });
                return notReady;
            }

            return CapabilityRequirementEvaluator.Evaluate(requirementSet, character.CreateRequirementContext());
        }
    }
}
