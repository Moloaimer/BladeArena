using UnityEngine;

namespace BladeArena
{
    public sealed class GateProjectile : MonoBehaviour
    {
        public Transform warning;
        public PixelSpellVisual visual;
        [Min(0)] public float warningDuration = 1.1f;
        [Min(.1f)] public float speed = 7;
        [Min(.1f)] public float lifetime = 5;
        [Min(.01f)] public float hitRadius = .17f;
        [System.NonSerialized] public Vector2 direction;
        [System.NonSerialized] public float age;
        [System.NonSerialized] public bool touchingPlayer;
        public bool Launched => age >= warningDuration;
        public void Step(float dt)
        {
            age += dt;
            if (warning) warning.gameObject.SetActive(!Launched);
            if (visual) visual.Step(age, Launched);
            if (Launched) transform.position += (Vector3)(direction * speed * Mathf.Min(dt, Mathf.Max(0, age - warningDuration)));
        }
    }
}
