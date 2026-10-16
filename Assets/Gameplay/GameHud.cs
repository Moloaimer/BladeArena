using UnityEngine;
using UnityEngine.UI;

namespace BladeArena
{
    public sealed class GameHud : MonoBehaviour
    {
        [Header("HUD 元素（可以改布局、字体、颜色）")]
        public Text stats, timer, notice, bossLabel;
        public Image experienceFill, bossFill;
        public Button pauseButton;
        public Text pauseLabel;
        public Image healthFill;
        public Text combatStats;
        [Header("Tab 属性与强化（保存的面板和 27 个槽位）")]
        public GameObject attributePanel;
        public Button attributeButton, closeAttributes;
        public Text attributeStats, attributeResources, ownedCount, tooltipTitle, tooltipBody;
        public OwnedUpgradeSlot[] ownedSlots;
        [Header("魔力与主动技能")]
        public GameObject skillBar;
        public Button refillButton, roundTableButton, excaliburButton;
        public Text manaLabel, activeSkillLabel;
        public Image[] manaFills;
        public Image holyFlash;
        [Header("暂停 / 升级 / 结算面板")]
        public GameObject pausePanel, upgradePanel, deathPanel;
        public Button resumeButton, restartButton;
        public Text result;
        public Button[] upgradeButtons;
        public Text[] upgradeTitles, upgradeDescriptions;
        [Header("虚拟摇杆")]
        public RectTransform joystick, joystickKnob;
        BladeGameController game;
        bool attributesShown;
        public void Bind(BladeGameController controller)
        {
            game = controller;
            pauseButton.onClick.AddListener(game.TogglePause);
            resumeButton.onClick.AddListener(game.TogglePause);
            restartButton.onClick.AddListener(game.RestartRun);
            if (attributeButton) attributeButton.onClick.AddListener(game.ToggleAttributes);
            if (closeAttributes) closeAttributes.onClick.AddListener(game.ToggleAttributes);
            if (refillButton) refillButton.onClick.AddListener(() => game.TryRefillSwords());
            if (roundTableButton) roundTableButton.onClick.AddListener(() => game.TryRoundTable());
            if (excaliburButton) excaliburButton.onClick.AddListener(() => game.TryExcalibur());
            for (int i = 0; i < upgradeButtons.Length; i++) { int index = i; upgradeButtons[i].onClick.AddListener(() => game.ChooseUpgrade(index)); }
            Refresh();
        }
        void LateUpdate() { if (game) Refresh(); }
        static string Clock(float seconds) => ((int)seconds / 60).ToString("00") + ":" + ((int)seconds % 60).ToString("00");
        public void Refresh()
        {
            var p = game.player;
            stats.text = "生命 " + p.health + "/" + p.maximumHealth + "    剑 " + p.Blades.Count + "/" + game.MaxBladeCount;
            if (skillBar)
            {
                skillBar.SetActive(game.ManaSlots > 0);
                manaLabel.text = "魔力 " + game.Mana.ToString("0.00") + " / " + game.ManaSlots;
                for (int i = 0; i < manaFills.Length; i++) manaFills[i].fillAmount = Mathf.Clamp01(game.Mana - i);
                refillButton.interactable = game.CanRefill;
                roundTableButton.interactable = game.CanRoundTable;
                excaliburButton.interactable = game.CanExcalibur;
                activeSkillLabel.text = game.IsCharging ? "圣剑蓄力 " + game.ChargeRemaining.ToString("0.0") + "秒" : game.RoundTableActive ? "剑阵 " + game.RoundTableRemaining.ToString("0.0") + "秒" : "每 100 秒恢复 1 格";
            }
            if (holyFlash) { var color = holyFlash.color; color.a = Mathf.Clamp01(game.HolyFlashRemaining / .3f) * .75f; holyFlash.color = color; }
            timer.text = "LV " + game.Level + "   /   " + Clock(game.Elapsed) + "   /   击杀 " + game.Kills;
            if (healthFill) healthFill.fillAmount = (float)p.health / p.maximumHealth;
            if (combatStats) combatStats.text = "攻击力 " + game.AttackPower.ToString("0.#") + "   暴击率 " + (game.CriticalChance * 100).ToString("0") + "%   护盾 " + game.Shields;
            if (attributePanel)
            {
                bool open = game.State == RunState.Attributes;
                attributePanel.SetActive(open);
                if (open && !attributesShown) RefreshAttributes();
                attributesShown = open;
            }
            notice.text = game.Notice;
            experienceFill.fillAmount = Mathf.Clamp01(game.Experience / game.NextExperience);
            pauseLabel.text = game.State == RunState.Paused ? "继续" : "暂停";
            pauseButton.interactable = game.State == RunState.Playing || game.State == RunState.Paused;
            pausePanel.SetActive(game.State == RunState.Paused);
            upgradePanel.SetActive(game.State == RunState.Upgrade);
            deathPanel.SetActive(game.State == RunState.Dead);
            bossFill.transform.parent.gameObject.SetActive(game.Boss);
            bossLabel.text = game.Boss ? "ARCHER · 吉尔伽美什  " + game.Boss.health + "/" + game.Boss.maximumHealth : "英雄王降临 " + Clock(Mathf.Max(0, game.NextBossTime - game.Elapsed)) + "  · 已击败 " + game.BossesDefeated;
            if (game.Boss) bossFill.fillAmount = (float)game.Boss.health / game.Boss.maximumHealth;
            if (game.State == RunState.Upgrade)
                for (int i = 0; i < 3; i++)
                {
                    bool valid = game.Choices[i] >= 0;
                    upgradeButtons[i].gameObject.SetActive(valid);
                    if (!valid) continue;
                    var d = game.Definition((UpgradeId)game.Choices[i]);
                    upgradeTitles[i].text = (i + 1) + "  [" + d.RarityLabel + "] " + d.title + "  Lv." + (game.UpgradeLevel(d.id) + 1);
                    upgradeTitles[i].color = d.RarityColor;
                    upgradeDescriptions[i].text = game.UpgradeDescription(d);
                }
            result.text = "生存 " + Clock(game.Elapsed) + "    击杀 " + game.Kills + "\n等级 " + game.Level + "    最佳 " + Clock(game.Best);
            joystick.gameObject.SetActive(game.Dragging && game.State == RunState.Playing);
            if (joystick.gameObject.activeSelf)
            {
                var parent = (RectTransform)joystick.parent;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, game.DragOrigin, null, out Vector2 origin);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, game.DragCurrent, null, out Vector2 current);
                joystick.localPosition = origin;
                joystickKnob.anchoredPosition = Vector2.ClampMagnitude(current - origin, 60);
            }
        }
        public void RefreshAttributes()
        {
            attributeStats.text = game.AttributeSummary;
            attributeResources.text = game.AttributeResources;
            int index = 0, ranks = 0;
            foreach (var d in game.settings.upgrades)
            {
                int level = game.UpgradeLevel(d.id);
                if (level <= 0) continue;
                ranks += level;
                if (index >= ownedSlots.Length) continue;
                ownedSlots[index].gameObject.SetActive(true);
                ownedSlots[index++].Bind(this, d, level);
            }
            for (int i = index; i < ownedSlots.Length; i++) ownedSlots[i].gameObject.SetActive(false);
            ownedCount.text = "已获强化 " + index + " / " + game.settings.upgrades.Length + "     总等级 " + ranks + (index == 0 ? "\n击败敌人，升级后选择强化。" : "");
            ShowUpgrade(null);
        }
        public void ShowUpgrade(UpgradeDefinition d)
        {
            tooltipTitle.text = d ? "[" + d.RarityLabel + "] " + d.title + "  Lv." + game.UpgradeLevel(d.id) : "强化详情";
            tooltipTitle.color = d ? d.RarityColor : new Color(.52f, .9f, .88f);
            tooltipBody.text = game.OwnedUpgradeDescription(d);
        }
    }
}
