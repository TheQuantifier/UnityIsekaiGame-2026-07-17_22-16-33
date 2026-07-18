using UnityEngine;

namespace UnityIsekaiGame.Combat
{
    public readonly struct DamageInfo
    {
        public DamageInfo(float rawAmount, GameObject source, Vector3 hitPoint, Vector3 hitDirection, DamageType damageType)
        {
            RawAmount = rawAmount;
            Source = source;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            DamageType = damageType;
        }

        public float RawAmount { get; }
        public GameObject Source { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitDirection { get; }
        public DamageType DamageType { get; }
    }
}
