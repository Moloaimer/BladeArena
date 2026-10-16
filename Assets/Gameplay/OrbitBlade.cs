using UnityEngine;

namespace BladeArena
{
    public sealed class OrbitBlade : MonoBehaviour
    {
        public SpriteRenderer artwork;
        [Range(0, 360)] public float angleOffset;
        [Min(.01f)] public float hitRadius = .17f;
        // A contact is consumed on entry, even if a shield/invulnerability blocks it.
        public readonly System.Collections.Generic.HashSet<int> BodyContacts = new System.Collections.Generic.HashSet<int>();
        public int Ring { get; set; }
        Vector3 baseScale;
        bool scaleCached;
        public float CollisionRadius => hitRadius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        public void SetSize(float multiplier)
        {
            if (!scaleCached) { baseScale = transform.localScale; scaleCached = true; }
            transform.localScale = baseScale * multiplier;
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, CollisionRadius);
        }
    }
}
