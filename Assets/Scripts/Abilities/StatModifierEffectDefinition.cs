using UnityEngine;
using UnityIsekaiGame.Equipment;

namespace UnityIsekaiGame.Abilities
{
    [CreateAssetMenu(fileName = "NewStatModifierEffect", menuName = "Unity Isekai Game/Abilities/Effects/Stat Modifier")]
    public sealed class StatModifierEffectDefinition : EffectDefinition
    {
        [SerializeField] private StatModifiers statModifiers;

        public StatModifiers StatModifiers => statModifiers;

        public override EffectExecutionResult CanExecute(in EffectExecutionContext context)
        {
            return EffectExecutionResult.Success($"{DisplayName} is configuration-only until status effects exist.");
        }

        public override EffectExecutionResult Execute(in EffectExecutionContext context)
        {
            return EffectExecutionResult.Failure(EffectExecutionStatus.NoStateChange, $"{DisplayName} does not execute without a status-effect runtime owner.");
        }
    }
}
