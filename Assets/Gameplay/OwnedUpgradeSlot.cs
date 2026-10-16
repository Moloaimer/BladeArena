using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BladeArena
{
    public sealed class OwnedUpgradeSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
    {
        public Text label;
        public Image accent;
        GameHud hud;
        UpgradeDefinition definition;
        public void Bind(GameHud owner, UpgradeDefinition upgrade, int level)
        {
            hud = owner; definition = upgrade;
            label.text = upgrade.title + "  Lv." + level;
            label.color = upgrade.RarityColor; accent.color = upgrade.RarityColor;
        }
        public void OnPointerEnter(PointerEventData e) { if (hud) hud.ShowUpgrade(definition); }
        public void OnSelect(BaseEventData e) { if (hud) hud.ShowUpgrade(definition); }
        public void OnPointerExit(PointerEventData e) { if (hud) hud.ShowUpgrade(null); }
    }
}
