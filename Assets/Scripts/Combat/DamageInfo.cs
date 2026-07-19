using UnityEngine;

namespace UnityIsekaiGame.Combat
{
    public readonly struct DamageInfo
    {
        public DamageInfo(float rawAmount, GameObject source, Vector3 hitPoint, Vector3 hitDirection, DamageType damageType)
            : this(rawAmount, source, hitPoint, hitDirection, damageType, DamagePacket.Single(source, DamageComponent.Legacy(damageType, rawAmount)))
        {
        }

        public DamageInfo(float rawAmount, GameObject source, Vector3 hitPoint, Vector3 hitDirection, DamageType damageType, DamagePacket damagePacket)
        {
            RawAmount = rawAmount;
            Source = source;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            DamageType = damageType;
            DamagePacket = damagePacket.HasComponents
                ? damagePacket
                : DamagePacket.Single(source, DamageComponent.Legacy(damageType, rawAmount));
        }

        public float RawAmount { get; }
        public GameObject Source { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitDirection { get; }
        public DamageType DamageType { get; }
        public DamagePacket DamagePacket { get; }
    }
}
