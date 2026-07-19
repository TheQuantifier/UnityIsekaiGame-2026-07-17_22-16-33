using System;
using UnityEngine;

namespace UnityIsekaiGame.Abilities
{
    [Serializable]
    public struct AbilityResourceCost
    {
        [SerializeField] private AbilityResourceType resourceType;
        [SerializeField, Min(0f)] private float amount;

        public AbilityResourceType ResourceType => resourceType;
        public float Amount => Mathf.Max(0f, amount);
        public bool HasInvalidRawAmount => amount < 0f || float.IsNaN(amount) || float.IsInfinity(amount);

        public void Validate()
        {
            amount = Mathf.Max(0f, amount);
        }
    }
}
