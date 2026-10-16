using UnityEngine;

namespace BladeArena
{
    public sealed partial class BladeGameController
    {
        static string Percent(float value) => (value * 100).ToString("0.#") + "%";
        public string AttributeSummary =>
            "生命     " + player.health + " / " + player.maximumHealth +
            "\n持剑数 / 剑阵容量 " + player.Blades.Count + " / " + MaxBladeCount + "  · " + player.RingCount + " 圈" +
            "\n攻击力   " + AttackPower.ToString("0.#") +
            "\n直接伤害加成 +" + Percent(DirectDamageBonus) +
            "\n精英 / Boss 加成 +" + Percent(EliteDamageBonus) +
            "\n每层连击加成 +" + Percent(Value(UpgradeId.Combo)) +
            "\n暴击率   " + Percent(CriticalChance) +
            "\n累计暴击率   " + Percent(RawCriticalChance) +
            "\n暴击伤害 " + Percent(CriticalMultiplier) +
            "\n转速     " + player.rotationSpeed.ToString("0.#") + " °/秒" +
            "\n环绕半径 " + player.orbitRadius.ToString("0.##") + " 米" +
            "\n剑刃尺寸 " + Percent(player.BladeScale) +
            "\n移动速度 " + player.moveSpeed.ToString("0.#") + " 米/秒" +
            "\n护盾     " + Shields + " / " + ShieldCapacity +
            "\n剑气生成率 " + Percent(WaveGenerationRate) +
            "\n反射生成率 " + Percent(ReflectionRate) +
            "\n快速重铸概率 " + Percent(QuickReforgeChance) +
            "\n重铸进度/次 " + Percent(MagicReforgeCredit) +
            "\n格挡无敌 " + ParryDuration.ToString("0.###") + " 秒";
        public string AttributeResources =>
            "回收能量  " + AvalonEnergy.ToString("0.#") + " / " + settings.avalonEnergyForShield +
            "\n重铸进度  " + Percent(reforgeProgress) +
            "\n魔力  " + Mana.ToString("0.00") + " / " + ManaSlots +
            "\nAvalon  " + (!Has(UpgradeId.Avalon) ? "未解锁" : AvalonReadyIn > 0 ? AvalonReadyIn.ToString("0.0") + " 秒" : "就绪 · 需 " + AvalonBladeCost + " 把剑");
        public string OwnedUpgradeDescription(UpgradeDefinition d)
        {
            if (!d) return "鼠标悬停已获强化，查看当前效果。\n蓄魔只可获得一次；其余强化可重复升级。";
            int level = UpgradeLevel(d.id);
            string current;
            switch (d.id)
            {
                case UpgradeId.Capacity: current = "剑阵容量 " + MaxBladeCount; break;
                case UpgradeId.Damage: current = "攻击力 " + AttackPower.ToString("0.#"); break;
                case UpgradeId.OrbitSpeed: current = "当前转速 " + player.rotationSpeed.ToString("0.#") + " °/秒"; break;
                case UpgradeId.Radius: current = "当前半径 " + player.orbitRadius.ToString("0.##") + " 米"; break;
                case UpgradeId.Width: current = "剑刃尺寸 " + Percent(player.BladeScale); break;
                case UpgradeId.WaveGeneration: current = "每次必定发射 " + Mathf.FloorToInt(WaveGenerationRate) + " 道，另有 " + Percent(WaveGenerationRate % 1) + " 概率多生成一道；预计伤害 " + PreviewDamage().ToString("0.#") + "（不含对精英的加成）"; break;
                case UpgradeId.Health: current = "生命上限 " + player.maximumHealth; break;
                case UpgradeId.Critical: current = "累计暴击率 " + Percent(RawCriticalChance) + " → 实际 " + Percent(CriticalChance) + " / 暴击伤害 " + Percent(CriticalMultiplier); break;
                case UpgradeId.ArmorPiercing: current = "对精英和 Boss 的伤害加成 +" + Percent(EliteDamageBonus); break;
                case UpgradeId.MoveSpeed: current = "移动速度 " + player.moveSpeed.ToString("0.##") + " 米/秒"; break;
                case UpgradeId.Recycle: current = "每次碎剑回收 " + Value(d.id).ToString("0.#") + " 能量；护盾容量 " + ShieldCapacity; break;
                case UpgradeId.Reforge: current = "重铸概率 " + Percent(QuickReforgeChance); break;
                case UpgradeId.Parry: current = "无敌时间 " + ParryDuration.ToString("0.###") + " 秒；间隔 " + settings.parryCooldown + " 秒"; break;
                case UpgradeId.Knockback: current = "击退距离 " + SpatialValue(d.id).ToString("0.##") + " 米"; break;
                case UpgradeId.DoubleRing: current = "已解锁双圈；转速加成 +" + Percent(SpatialValue(d.id)); break;
                case UpgradeId.TripleRing: current = "已解锁三圈；半径加成 +" + Percent(SpatialValue(d.id)); break;
                case UpgradeId.Rally: current = "当前直接伤害加成 +" + Percent(DirectDamageBonus); break;
                case UpgradeId.Combo: current = "每层伤害加成 +" + Percent(Value(d.id)) + "，最多 " + settings.comboMaxStacks + " 层"; break;
                case UpgradeId.Reflect: current = "独立生成率 " + Percent(ReflectionRate) + "；伤害 " + PreviewDamage().ToString("0.#") + "，不暴击"; break;
                case UpgradeId.Excalibur: current = "消耗 3 格魔力；技能倍率 " + Percent(ExcaliburMultiplier) + "；预计伤害 " + PreviewDamage(ExcaliburMultiplier).ToString("0.#"); break;
                case UpgradeId.ShatterBurst:
                case UpgradeId.PiercingWave: current = "技能倍率 " + Percent(Value(d.id)) + "；预计伤害 " + PreviewDamage(Value(d.id)).ToString("0.#") + "（不含对精英的加成）"; break;
                case UpgradeId.LakeBlessing: current = "格挡计数 " + blockedEnemyBlades + " / " + d.amount + "；护盾容量 " + ShieldCapacity; break;
                case UpgradeId.MagicReforge: current = "未触发快速重铸时，每次碎剑增加 " + Percent(MagicReforgeCredit) + " 进度"; break;
                case UpgradeId.RoundTable: current = "消耗 2 格魔力；持续 " + RoundTableDuration.ToString("0.#") + " 秒，剑不会被撞碎"; break;
                case UpgradeId.Avalon: current = "消耗 " + AvalonBladeCost + " 把；下次触发后的冷却为 " + AvalonCooldown.ToString("0.##") + " 秒"; break;
                case UpgradeId.ManaReservoir: current = "魔力 " + Mana.ToString("0.00") + " / " + ManaSlots + "；E 消耗 1 格，补满剑阵"; break;
                default: current = "当前等级 " + level; break;
            }
            return current + "\n" + d.description;
        }
    }
}
