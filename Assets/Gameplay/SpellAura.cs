using UnityEngine;

namespace BladeArena
{
    public sealed class SpellAura : MonoBehaviour
    {
        public Transform seal;
        public SpriteRenderer[] rays;
        public void Show(float progress)
        {
            seal.localRotation = Quaternion.Euler(0, 0, Mathf.Floor(progress * 12) * 15);
            seal.localScale = Vector3.one * (1 + progress * .4f);
            for (int i = 0; i < rays.Length; i++)
            {
                var color = rays[i].color; color.a = .3f + progress * .65f; rays[i].color = color;
            }
        }
    }
}
