using UnityEngine;

namespace BladeArena
{
    public enum UpgradeRarity { Common, Rare, Special }
    public enum UpgradeId
    {
        Capacity, Damage, OrbitSpeed, Radius, Width, WaveGeneration, Health,
        ArmorPiercing, MoveSpeed = 9, Critical, Recycle, Reforge, Parry, Knockback,
        DoubleRing, TripleRing, Rally, PiercingWave, Combo, ShatterBurst,
        LakeBlessing, MagicReforge, Reflect, RoundTable, Excalibur, Avalon, ManaReservoir
    }
    [CreateAssetMenu(menuName = "Blade Arena/强化", fileName = "Upgrade")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        public UpgradeId id;
        public string title;
        [TextArea(2, 5)] public string description;
        public UpgradeRarity rarity;
        [Tooltip("除 Unique 强化外可重复升级。百分比用小数；具体成长方式见战斗与强化说明。")]
        public float amount;
        public float perExtraLevel;
        public bool unique;
        public UpgradeId[] prerequisites = new UpgradeId[0];
        [Min(0)] public int requiredBladeCount;
        public string RarityLabel => rarity == UpgradeRarity.Common ? "普通" : rarity == UpgradeRarity.Rare ? "稀有" : "特殊";
        public Color RarityColor => rarity == UpgradeRarity.Common ? new Color(.65f, 1, .85f) : rarity == UpgradeRarity.Rare ? new Color(.68f, .62f, 1) : new Color(1, .8f, .3f);
    }
}
