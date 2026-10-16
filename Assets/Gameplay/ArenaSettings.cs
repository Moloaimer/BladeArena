using UnityEngine;

namespace BladeArena
{
    [CreateAssetMenu(menuName = "Blade Arena/规则配置", fileName = "ArenaSettings")]
    public sealed class ArenaSettings : ScriptableObject
    {
        [Header("竞技场与数量上限")]
        [Min(5)] public float arenaRadius = 22;
        [Tooltip("玩家初始剑阵容量，可通过剑阵扩充无限增长。")]
        [Min(1)] public int bladeLimit = 30;
        [Range(8, 64)] public int enemyBladeLimit = 48;
        [Range(1, 60)] public int enemyLimit = 32;
        [Range(1, 100)] public int orbLimit = 60;
        [Header("生成节奏")]
        [Min(.1f)] public float enemySpawnInterval = 3.2f;
        [Min(.1f)] public float minimumSpawnInterval = .55f;
        [Min(.1f)] public float orbSpawnInterval = .65f;
        [Min(0)] public float eliteStartTime = 40;
        [Range(0, 1)] public float eliteChance = .16f;
        [Header("拾取与成长")]
        [Min(.5f)] public float pickupRadius = 1.3f;
        [Min(1)] public int firstLevelExperience = 5;
        [Min(1)] public int experienceStep = 3;
        [Min(.05f)] public float hurtInvulnerability = .8f;
        [Header("Boss")]
        [Min(1)] public float firstBossTime = 60;
        [Min(1)] public float bossInterval = 90;
        [Min(0)] public int bossHealthGrowth = 400;
        [Min(.1f)] public float volleyInterval = 4.2f;
        [Min(.1f)] public float enragedVolleyInterval = 2.8f;
        [Min(1)] public int bossExperience = 12;
        [Min(0)] public int bossBladeReward = 8;
        [Header("强化池（可直接编辑每个强化资源）")]
        public UpgradeDefinition[] upgrades;
        public SwordWave swordWavePrefab;
        public SwordWave twinSwordPrefab, reflectedWavePrefab, holySwordPrefab;
        public PixelBurst impactPrefab;
        [UnityEngine.Serialization.FormerlySerializedAs("evolutionWeight")]
        [Min(0)] public float specialWeight = 5;
        [Min(0)] public float commonWeight = 75, rareWeight = 20;
        [Header("流派机制")]
        [Min(1)] public int waveHitThreshold = 8;
        [Min(.01f)] public float comboWindow = 1.5f;
        [Min(1)] public int comboMaxStacks = 5;
        [Min(.1f)] public float burstRadius = 2, knockbackRadius = 2.5f;
        [Min(.1f)] public float reforgeDelay = 3;
        [Min(1)] public float avalonEnergyForShield = 100;
        [Tooltip("基础护盾容量；湖之加护与魔力回收每级额外 +1，并非最终上限。")]
        [Min(1)] public int shieldLimit = 3;
        [Min(.1f)] public float avalonInvulnerability = 2;
        [Header("魔力与主动技能")]
        public GameAudio soundPrefab;
        public SpellAura spellAuraPrefab;
        [Min(1)] public int manaCapacity = 3;
        [Min(0)] public float manaPerSecond = .01f, manaPerKill = .05f;
        [Min(1)] public float roundTableRadiusMultiplier = 1.5f, roundTableSpeedMultiplier = 1.6f;
        [Min(.1f)] public float excaliburChargeDuration = 1, excaliburPushRadius = 4;
        [Header("暴击与防御平衡")]
        [Min(0)] public float baseCriticalChance = 0;
        [Min(1)] public float baseCriticalMultiplier = 2;
        [Min(0)] public float criticalOverflowConversion = 1;
        [Min(.36f)] public float parryCooldown = .6f;
        [Min(.1f)] public float avalonCooldown = 60;
        [Range(0, 1)] public float avalonBladeCostFraction = .2f;
        [Header("无尽敌人成长（每分钟，按出生时计算）")]
        [Min(0)] public float enemyHealthGrowthPerMinute = .25f;
        [Min(0)] public float enemyDamageGrowthPerMinute = .1f;
    }
}
