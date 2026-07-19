using System;
using UnityEngine;
using UnityIsekaiGame.Combat;

namespace UnityIsekaiGame.Equipment
{
    [Serializable]
    public sealed class EquipmentData
    {
        [SerializeField] private bool equippable;
        [SerializeField] private EquipmentSlotType slotType;
        [SerializeField] private StatModifiers statModifiers;
        [SerializeField] private ResistanceModifierDefinition[] resistanceModifiers;
        [SerializeField] private MeleeWeaponData meleeWeapon;

        public bool Equippable => equippable;
        public EquipmentSlotType SlotType => slotType;
        public StatModifiers StatModifiers => statModifiers;
        public System.Collections.Generic.IReadOnlyList<ResistanceModifierDefinition> ResistanceModifiers => resistanceModifiers ?? Array.Empty<ResistanceModifierDefinition>();
        public MeleeWeaponData MeleeWeapon => meleeWeapon;

        public void Validate()
        {
            meleeWeapon?.Validate();
        }
    }
}
