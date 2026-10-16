using UnityEngine;

namespace BladeArena
{
    public sealed class PickupOrb : MonoBehaviour
    {
        [Range(1, 10)] public int value = 1;
        public TextMesh valueLabel;
        public void RefreshLabel() { if (valueLabel) valueLabel.text = "+" + value; }
        void OnValidate() { RefreshLabel(); }
        void Awake() { RefreshLabel(); }
    }
}
