using System.Collections.Generic;
using UnityEngine;

namespace BladeArena
{
    public sealed partial class BladeGameController
    {
        readonly Dictionary<UpgradeId, int> upgradeLevels = new Dictionary<UpgradeId, int>();
        readonly Dictionary<UpgradeId, UpgradeDefinition> definitions = new Dictionary<UpgradeId, UpgradeDefinition>();
        float baseDamage, baseRotation, baseRadius, baseMoveSpeed;
        int baseHealth;
        public int MaxBladeCount { get; private set; }
        public float AttackPower => player.bladeDamage;
        public float DirectDamageBonus => player.Blades.Count / 10 * Value(UpgradeId.Rally);
        public float EliteDamageBonus => SpatialValue(UpgradeId.ArmorPiercing);
        public float PreviewDamage(float skillMultiplier = 1, bool direct = false) => AttackPower * skillMultiplier * (1 + (direct ? DirectDamageBonus : 0));
        public int Shields { get; private set; }
        public float AvalonEnergy { get; private set; }
        public int UpgradeLevel(UpgradeId id) => upgradeLevels.TryGetValue(id, out int level) ? level : 0;
        public bool Has(UpgradeId id) => UpgradeLevel(id) > 0;
        public UpgradeDefinition Definition(UpgradeId id) => definitions.TryGetValue(id, out var d) ? d : null;
        public float Value(UpgradeId id) => definitions.TryGetValue(id, out var d) ? UpgradeLevel(id) * d.amount : 0;
        public float WaveGenerationRate => Has(UpgradeId.WaveGeneration) ? Definition(UpgradeId.WaveGeneration).amount + (UpgradeLevel(UpgradeId.WaveGeneration) - 1) * Definition(UpgradeId.WaveGeneration).perExtraLevel : 0;
        public float RawCriticalChance => settings.baseCriticalChance + Value(UpgradeId.Critical);
        public float CriticalChance => Mathf.Clamp01(RawCriticalChance);
        public float CriticalMultiplier => settings.baseCriticalMultiplier + Mathf.Max(0, RawCriticalChance - 1) * settings.criticalOverflowConversion;
        public int ShieldCapacity => settings.shieldLimit + UpgradeLevel(UpgradeId.LakeBlessing) + UpgradeLevel(UpgradeId.Recycle);
        public float QuickReforgeChance => RecoveryChance(UpgradeId.Reforge);
        public float MagicReforgeCredit => RecoveryChance(UpgradeId.MagicReforge);
        public float ParryDuration => Has(UpgradeId.Parry) ? Definition(UpgradeId.Parry).amount + .15f * (UpgradeLevel(UpgradeId.Parry) - 1f) / (UpgradeLevel(UpgradeId.Parry) + 4f) : 0;
        public float AvalonCooldown => settings.avalonInvulnerability + 1 + Mathf.Max(0, settings.avalonCooldown - settings.avalonInvulnerability - 1) / (1 + Mathf.Max(0, UpgradeLevel(UpgradeId.Avalon) - 1) * (Definition(UpgradeId.Avalon) ? Definition(UpgradeId.Avalon).amount : 0));
        public float AvalonReadyIn => Mathf.Max(0, avalonReadyAt - Elapsed);
        public int AvalonBladeCost => Mathf.Max(5, Mathf.CeilToInt(player.Blades.Count * settings.avalonBladeCostFraction));
        public float SpatialValue(UpgradeId id) => Definition(id) ? Definition(id).amount * Mathf.Sqrt(UpgradeLevel(id)) : 0;
        float RecoveryChance(UpgradeId id)
        {
            if (!Has(id)) return 0;
            float first = Mathf.Clamp(Definition(id).amount, 0, .99f);
            float rating = first * UpgradeLevel(id);
            return rating / (1 - first + rating);
        }
        public static int RandomYield(float expected)
        {
            expected = Mathf.Max(0, expected);
            int guaranteed = Mathf.FloorToInt(expected);
            return guaranteed + (Random.value < expected - guaranteed ? 1 : 0);
        }
        public float ResolveCritical(float damage, float roll) => CriticalChance >= 1 || roll < CriticalChance ? damage * CriticalMultiplier : damage;
        public string UpgradeDescription(UpgradeDefinition d)
        {
            int next = UpgradeLevel(d.id) + 1;
            string detail = "";
            if (d.id == UpgradeId.Capacity) detail = "剑阵容量 " + MaxBladeCount + " → " + (MaxBladeCount + Mathf.RoundToInt(d.amount));
            if (d.id == UpgradeId.Critical)
            {
                float raw = settings.baseCriticalChance + next * d.amount;
                detail = "下一级：暴击率 " + (Mathf.Clamp01(raw) * 100).ToString("0") + "% · 暴击伤害 " + ((settings.baseCriticalMultiplier + Mathf.Max(0, raw - 1) * settings.criticalOverflowConversion) * 100).ToString("0") + "%";
            }
            if (d.id == UpgradeId.WaveGeneration)
            {
                float yield = d.amount + (next - 1) * d.perExtraLevel;
                detail = "下一级：必定发射 " + Mathf.FloorToInt(yield) + " 道，另有 " + ((yield % 1) * 100).ToString("0") + "% 概率多生成 1 道";
            }
            return d.description + (detail.Length > 0 ? "\n" + detail : "");
        }

        void InitializeProgression()
        {
            upgradeLevels.Clear(); definitions.Clear();
            if (settings.upgrades != null)
                foreach (var d in settings.upgrades) if (d && !definitions.ContainsKey(d.id)) definitions.Add(d.id, d);
            baseDamage = player.bladeDamage; baseHealth = player.maximumHealth;
            baseRotation = player.rotationSpeed; baseRadius = player.orbitRadius; baseMoveSpeed = player.moveSpeed;
            MaxBladeCount = settings.bladeLimit;
            for (int i = 0; i < Choices.Length; i++) Choices[i] = -1;
        }
        public void RecalculateStats()
        {
            MaxBladeCount = settings.bladeLimit + Mathf.RoundToInt(Value(UpgradeId.Capacity));
            player.bladeDamage = baseDamage * (1 + SpatialValue(UpgradeId.Damage));
            player.rotationSpeed = baseRotation * (1 + SpatialValue(UpgradeId.OrbitSpeed) + SpatialValue(UpgradeId.DoubleRing)) * (RoundTableActive ? settings.roundTableSpeedMultiplier : 1);
            player.orbitRadius = baseRadius * (1 + SpatialValue(UpgradeId.Radius) + SpatialValue(UpgradeId.TripleRing)) * (RoundTableActive ? settings.roundTableRadiusMultiplier : 1);
            player.moveSpeed = baseMoveSpeed * (1 + SpatialValue(UpgradeId.MoveSpeed));
            player.BladeScale = 1 + SpatialValue(UpgradeId.Width);
            player.maximumHealth = Mathf.RoundToInt(baseHealth * (1 + Value(UpgradeId.Health)));
            player.health = Mathf.Min(player.health, player.maximumHealth);
            UpdateFormation(); player.PlaceBlades();
        }
        void UpdateFormation()
        {
            int rings = Has(UpgradeId.DoubleRing) ? 2 : 1;
            if (Has(UpgradeId.TripleRing)) rings = 3;
            player.DefensiveInnerRing = RoundTableActive;
            player.ChaoticFormation = RoundTableActive;
            if (player.RingCount != rings) { player.RingCount = rings; player.Rearrange(); }
        }
        public bool CanAcquire(UpgradeDefinition d)
        {
            if (!d || (d.unique && Has(d.id)) || (!Has(d.id) && player.Blades.Count < d.requiredBladeCount)) return false;
            foreach (var prerequisite in d.prerequisites) if (!Has(prerequisite)) return false;
            return true;
        }
        public bool AcquireUpgrade(UpgradeId id)
        {
            var d = Definition(id);
            if (!CanAcquire(d)) return false;
            upgradeLevels[id] = UpgradeLevel(id) + 1;
            if (id == UpgradeId.LakeBlessing && UpgradeLevel(id) == 1) blockedEnemyBlades = 0;
            RecalculateStats();
            if (id == UpgradeId.Health) player.health = Mathf.Min(player.maximumHealth, player.health + Mathf.RoundToInt(player.maximumHealth * d.perExtraLevel));
            return true;
        }
        public void CollectOrb(int value)
        {
            player.AddBlades(Mathf.Max(0, value), MaxBladeCount);
            UpdateFormation(); PlaySound(SoundCue.Pickup);
        }
        public void OfferUpgrade()
        {
            var pool = new List<UpgradeDefinition>();
            foreach (var d in definitions.Values) if (CanAcquire(d)) pool.Add(d);
            if (pool.Count == 0)
            {
                // A custom pool may be empty or contain only currently locked definitions.
                player.health = Mathf.Min(player.maximumHealth, player.health + Mathf.CeilToInt(player.maximumHealth * .25f));
                FinishLevel(); SetNotice("当前无可选强化 · 恢复 25% 生命"); return;
            }
            State = RunState.Upgrade; ResetDrag();
            PlaySound(SoundCue.Upgrade);
            for (int i = 0; i < Choices.Length; i++)
            {
                Choices[i] = -1;
                if (pool.Count == 0) continue;
                // Roll rarity first, so adding common definitions doesn't dilute rare probabilities.
                float total = 0;
                var weights = new float[3];
                for (int r = 0; r < 3; r++)
                {
                    if (!pool.Exists(d => (int)d.rarity == r)) continue;
                    weights[r] = Mathf.Max(0, r == 0 ? settings.commonWeight : r == 1 ? settings.rareWeight : settings.specialWeight);
                    total += weights[r];
                }
                UpgradeDefinition selected;
                if (total <= 0) selected = pool[Random.Range(0, pool.Count)];
                else
                {
                    float roll = Random.value * total; int rarity = 0;
                    for (; rarity < 2; rarity++) { if (roll < weights[rarity]) break; roll -= weights[rarity]; }
                    var tier = pool.FindAll(d => (int)d.rarity == rarity);
                    selected = tier.Count > 0 ? tier[Random.Range(0, tier.Count)] : pool[0];
                }
                Choices[i] = (int)selected.id; pool.Remove(selected);
            }
        }
        void FinishLevel()
        {
            Experience = Mathf.Max(0, Experience - NextExperience);
            NextExperience += settings.experienceStep; Level++; State = RunState.Playing;
        }
        public void ChooseUpgrade(int index)
        {
            if (State != RunState.Upgrade || index < 0 || index >= Choices.Length || Choices[index] < 0) return;
            var id = (UpgradeId)Choices[index];
            if (!AcquireUpgrade(id)) return;
            FinishLevel(); SetNotice(Definition(id).title + " · 等级 " + UpgradeLevel(id));
            if (Experience >= NextExperience) OfferUpgrade();
        }
        public string BuildStatus => "攻击力 " + AttackPower.ToString("0.#") + "  直接伤害加成 +" + (DirectDamageBonus * 100).ToString("0.#") + "%  护盾 " + Shields + "/" + ShieldCapacity;
    }
}
