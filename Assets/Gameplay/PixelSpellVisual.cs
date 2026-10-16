using UnityEngine;

namespace BladeArena
{
    // All sprites are saved children of the editable spell prefabs. Simulation drives the animation.
    public sealed class PixelSpellVisual : MonoBehaviour
    {
        public SpriteRenderer[] trailPixels;
        public Transform seal;
        public void Step(float age, bool launched = true)
        {
            for (int i = 0; i < trailPixels.Length; i++)
            {
                var pixel = trailPixels[i];
                pixel.gameObject.SetActive(launched);
                float tick = Mathf.Floor(age * 12 + i) % 4;
                var color = pixel.color; color.a = .28f + tick * .16f; pixel.color = color;
            }
            if (seal)
            {
                seal.gameObject.SetActive(!launched);
                seal.localRotation = Quaternion.Euler(0, 0, Mathf.Floor(age * 8) * 15);
            }
        }
    }
}
