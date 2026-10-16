using System.Collections.Generic;
using UnityEngine;

namespace BladeArena
{
    // Authored prefab shared by piercing, retaliation and the holy sword slash.
    public sealed class SwordWave : MonoBehaviour
    {
        public SpriteRenderer artwork;
        public float speed = 12, lifetime = .7f, hitRadius = .3f;
        public bool homing, singleHit;
        public bool canCrit = true;
        public PixelSpellVisual visual;
        [System.NonSerialized] public Combatant target;
        [System.NonSerialized] public Vector2 direction;
        [System.NonSerialized] public float skillMultiplier = 1, age;
        public readonly HashSet<int> HitTargets = new HashSet<int>();
        public float CollisionRadius => hitRadius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        public void Step(float dt)
        {
            age += dt;
            if (homing && target && target.alive)
            {
                Vector2 delta = target.Position - (Vector2)transform.position;
                if (delta.sqrMagnitude > .0001f) direction = delta.normalized;
            }
            transform.position += (Vector3)(direction * speed * dt);
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);
            if (visual) visual.Step(age);
        }
        void OnDrawGizmosSelected() { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, CollisionRadius); }
    }
}
