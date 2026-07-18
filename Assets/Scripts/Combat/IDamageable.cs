namespace UnityIsekaiGame.Combat
{
    public interface IDamageable
    {
        DamageResult ApplyDamage(in DamageInfo damageInfo);
    }
}
