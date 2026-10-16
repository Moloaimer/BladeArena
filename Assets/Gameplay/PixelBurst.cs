using UnityEngine;

namespace BladeArena
{
    public sealed class PixelBurst : MonoBehaviour
    {
        public SpriteRenderer[] sparks;
        public float lifetime = .24f, spread = 1;
        float age;
        Vector3[] initialScale;
        void Awake()
        {
            initialScale = new Vector3[sparks.Length];
            for (int i = 0; i < sparks.Length; i++) initialScale[i] = sparks[i].transform.localScale;
        }
        public bool Step(float dt)
        {
            age += dt;
            for (int i = 0; i < sparks.Length; i++)
            {
                float a = i * Mathf.PI * 2 / sparks.Length;
                sparks[i].transform.localPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Mathf.Floor(age * 40) / 10 * spread;
                sparks[i].transform.localScale = initialScale[i] * (1 - Mathf.Clamp01(age / lifetime));
            }
            return age >= lifetime;
        }
    }
}
