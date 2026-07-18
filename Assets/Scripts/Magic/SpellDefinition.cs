using UnityEngine;

namespace UnityIsekaiGame.Magic
{
    [CreateAssetMenu(fileName = "NewSpellDefinition", menuName = "Unity Isekai Game/Magic/Spell Definition")]
    public sealed class SpellDefinition : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField, Min(0f)] private float manaCost = 10f;
        [SerializeField, Min(0f)] private float cooldown = 0.5f;
        [SerializeField, Min(0f)] private float baseDamage = 10f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 16f;
        [SerializeField, Min(0.1f)] private float maximumLifetime = 3f;
        [SerializeField] private SpellProjectile projectilePrefab;
        [SerializeField] private Vector3 castPointOffset = new Vector3(0.2f, -0.15f, 0.4f);

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public float ManaCost => manaCost;
        public float Cooldown => cooldown;
        public float BaseDamage => baseDamage;
        public float ProjectileSpeed => projectileSpeed;
        public float MaximumLifetime => maximumLifetime;
        public SpellProjectile ProjectilePrefab => projectilePrefab;
        public Vector3 CastPointOffset => castPointOffset;

        private void OnValidate()
        {
            manaCost = Mathf.Max(0f, manaCost);
            cooldown = Mathf.Max(0f, cooldown);
            baseDamage = Mathf.Max(0f, baseDamage);
            projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
            maximumLifetime = Mathf.Max(0.1f, maximumLifetime);
        }
    }
}
