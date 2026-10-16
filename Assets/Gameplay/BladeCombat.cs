using System.Collections.Generic;
using UnityEngine;

namespace BladeArena
{
    public sealed partial class BladeGameController
    {
        struct ShatterEvent { public Vector2 position, direction; }
        sealed class ComboState
        {
            public float lastTime;
            public readonly HashSet<int> blades = new HashSet<int>();
        }
        readonly Queue<ShatterEvent> shatters = new Queue<ShatterEvent>();
        readonly List<float> reforges = new List<float>();
        readonly Dictionary<int, ComboState> combos = new Dictionary<int, ComboState>();
        public readonly List<SwordWave> Waves = new List<SwordWave>();
        readonly List<PixelBurst> bursts = new List<PixelBurst>();
        int blockedEnemyBlades, waveHits;
        float reforgeProgress, parryReadyAt, avalonReadyAt;

        static bool Overlaps(Vector2 a, Vector2 b, float radius) => (a - b).sqrMagnitude <= radius * radius;
        public void ResolveCombat(Combatant enemy)
        {
            if (!player.alive || !enemy.alive) return;
            ResolveBladeClashes(enemy);
            HitBody(player, enemy);
            if (enemy.alive && player.alive) HitBody(enemy, player);
        }
        void ResolveBladeClashes(Combatant enemy)
        {
            if (!player.alive || !enemy.alive || player.SwordsHidden) return;
            if (!Overlaps(player.Position, enemy.Position, Mathf.Max(2, player.OuterRadius) + enemy.OuterRadius + player.BladeScale + 1)) return;
            // For the evolved round table, inner blades get first chance to intercept.
            for (int ring = 0; ring < player.RingCount; ring++)
                for (int i = player.Blades.Count - 1; i >= 0; i--)
                {
                    var blade = player.Blades[i];
                    if (blade.Ring != ring) continue;
                    for (int j = enemy.Blades.Count - 1; j >= 0; j--)
                    {
                        var other = enemy.Blades[j];
                        if (!Overlaps(blade.transform.position, other.transform.position, blade.CollisionRadius + other.CollisionRadius)) continue;
                        Vector2 position = other.transform.position;
                        enemy.BreakBlade(j); BlockEnemyBlade(blade, position);
                        if (!RoundTableActive) break;
                    }
                }
        }
        public void HitBody(Combatant attacker, Combatant target)
        {
            if (!attacker.alive || !target.alive || attacker.SwordsHidden) return;
            for (int i = attacker.Blades.Count - 1; i >= 0 && target.alive; i--)
            {
                var blade = attacker.Blades[i];
                int targetId = target.GetInstanceID();
                bool overlaps = Overlaps(blade.transform.position, target.Position, blade.CollisionRadius + target.bodyHitRadius);
                if (!overlaps) { blade.BodyContacts.Remove(targetId); continue; }
                if (!blade.BodyContacts.Add(targetId)) continue;
                if (attacker.IsPlayer)
                {
                    if (Damage(target, CalculateBladeDamage(blade, target))) OnBladeHit(blade);
                }
                else
                {
                    // A normal hit retains the enemy blade. Blocked hits consume it.
                    if (!Damage(target, attacker.bladeDamage)) attacker.BreakBlade(i);
                }
            }
        }
        public float CalculateBladeDamage(OrbitBlade blade, Combatant target)
        {
            if (Has(UpgradeId.Combo))
            {
                int key = target.GetInstanceID();
                if (!combos.TryGetValue(key, out var combo)) { combo = new ComboState(); combos.Add(key, combo); }
                if (Elapsed - combo.lastTime > settings.comboWindow) combo.blades.Clear();
                combo.blades.Add(blade.GetInstanceID());
                combo.lastTime = Elapsed;
            }
            return ResolvePlayerDamage(target, 1, true, Random.value);
        }
        public float ComboDamageBonus(Combatant target)
        {
            if (!target || !combos.TryGetValue(target.GetInstanceID(), out var combo) || Elapsed - combo.lastTime > settings.comboWindow) return 0;
            return Mathf.Min(settings.comboMaxStacks, Mathf.Max(0, combo.blades.Count - 1)) * Value(UpgradeId.Combo);
        }
        public float ResolvePlayerDamage(Combatant target, float skillMultiplier, bool direct, float criticalRoll, bool canCrit = true)
        {
            // All attacks share one additive bonus pool; only their skill multipliers differ.
            float bonus = direct ? DirectDamageBonus + ComboDamageBonus(target) : 0;
            if (target && (target.role == CharacterRole.Elite || target.IsBoss)) bonus += EliteDamageBonus;
            float damage = AttackPower * skillMultiplier * (1 + bonus);
            return canCrit ? ResolveCritical(damage, criticalRoll) : damage;
        }
        void OnBladeHit(OrbitBlade blade)
        {
            GenerateSingleWaves(player.Position, WaveGenerationRate);
            if (Has(UpgradeId.PiercingWave) && ++waveHits >= settings.waveHitThreshold)
            {
                waveHits = 0;
                SpawnWave(blade.transform.position, ((Vector2)blade.transform.position - player.Position).normalized, Value(UpgradeId.PiercingWave));
            }
        }
        public Combatant NearestEnemy(Vector2 origin)
        {
            Combatant nearest = null; float distance = float.PositiveInfinity;
            foreach (var enemy in Enemies)
                if (enemy && enemy.alive && (enemy.Position - origin).sqrMagnitude < distance)
                { nearest = enemy; distance = (enemy.Position - origin).sqrMagnitude; }
            return nearest;
        }
        void GenerateSingleWaves(Vector2 origin, float rate, SwordWave source = null)
        {
            var prefab = source ? source : settings.twinSwordPrefab;
            if (rate <= 0 || !prefab) return;
            var target = NearestEnemy(origin); if (!target) return;
            int count = RandomYield(rate);
            for (int i = 0; i < count; i++)
            {
                // Compact launch fan; the homing sword is a one-hit spell, never an orbit resource.
                Vector2 offset = new Vector2(Mathf.Cos(i * 2.4f), Mathf.Sin(i * 2.4f)) * .18f;
                var wave = CreateWave(prefab, origin + offset, target.Position - origin, 1, 1);
                if (wave) { wave.target = target; wave.canCrit = false; }
            }
        }
        void BlockEnemyBlade(OrbitBlade blade, Vector2 enemyPosition)
        {
            int index = player.Blades.IndexOf(blade);
            if (index < 0) return;
            Vector2 position = blade.transform.position;
            // Counts include blocked Gate of Babylon swords. Sacrifices for Avalon never call this.
            blockedEnemyBlades++;
            if (Has(UpgradeId.Parry) && Elapsed >= parryReadyAt)
            {
                player.invulnerability = Mathf.Max(player.invulnerability, ParryDuration);
                parryReadyAt = Elapsed + settings.parryCooldown;
            }
            if (Has(UpgradeId.LakeBlessing) && blockedEnemyBlades >= Mathf.Max(1, Mathf.RoundToInt(Definition(UpgradeId.LakeBlessing).amount)))
            { blockedEnemyBlades = 0; GrantShield(1); }
            PlaySound(SoundCue.Clash);
            GenerateSingleWaves(position, ReflectionRate, settings.reflectedWavePrefab);
            if (RoundTableActive) return;
            player.BreakBlade(index);
            if (Has(UpgradeId.Recycle))
            {
                AvalonEnergy += Value(UpgradeId.Recycle);
                if (AvalonEnergy >= settings.avalonEnergyForShield)
                {
                    int charges = Mathf.FloorToInt(AvalonEnergy / settings.avalonEnergyForShield);
                    AvalonEnergy %= settings.avalonEnergyForShield; GrantShield(charges);
                }
            }
            // Both reforge paths share one refund budget per lost blade.
            if (Has(UpgradeId.Reforge) && Random.value < QuickReforgeChance) reforges.Add(Elapsed + settings.reforgeDelay);
            else if (Has(UpgradeId.MagicReforge))
            {
                reforgeProgress += MagicReforgeCredit;
                if (reforgeProgress >= 1 - .000001f) { reforgeProgress = Mathf.Max(0, reforgeProgress - 1); reforges.Add(Elapsed); }
            }
            Vector2 direction = enemyPosition - player.Position;
            shatters.Enqueue(new ShatterEvent { position = position, direction = direction.sqrMagnitude > .01f ? direction.normalized : player.Facing });
        }
        void GrantShield(int count) { Shields += Mathf.Min(count, Mathf.Max(0, ShieldCapacity - Shields)); }
        public bool Damage(Combatant target, float amount = 10)
        {
            if (!target.alive || target.invulnerability > 0 || amount <= 0) return false;
            int damage = Mathf.Max(1, Mathf.RoundToInt(amount));
            if (target.IsPlayer)
            {
                if (Shields > 0) { Shields--; target.invulnerability = settings.hurtInvulnerability; SetNotice("护盾抵挡伤害"); PlaySound(SoundCue.Shield); return false; }
                if (target.health <= damage && Has(UpgradeId.Avalon) && AvalonReadyIn <= 0 && player.Blades.Count >= AvalonBladeCost)
                {
                    int cost = AvalonBladeCost;
                    for (int i = 0; i < cost; i++) player.BreakBlade(player.Blades.Count - 1);
                    target.health = 1; target.invulnerability = settings.avalonInvulnerability;
                    avalonReadyAt = Elapsed + AvalonCooldown;
                    SetNotice("Avalon · 消耗 " + cost + " 把剑抵挡致命伤害", 3); return false;
                }
                target.invulnerability = settings.hurtInvulnerability;
            }
            target.health = Mathf.Max(0, target.health - damage);
            target.FlashHit(); PlaySound(target.IsPlayer ? SoundCue.Hurt : SoundCue.Hit);
            if (target.health > 0) return true;
            target.alive = false; target.gameObject.SetActive(false);
            if (target.IsPlayer) { CancelActiveSkills(); PlaySound(SoundCue.Death); return true; }
            AddMana(settings.manaPerKill);
            Burst(target.Position, new Color(1, .3f, .3f));
            int id = target.GetInstanceID(); combos.Remove(id);
            foreach (var blade in player.Blades) blade.BodyContacts.Remove(id);
            Kills++; Experience += target.IsBoss ? settings.bossExperience : target.role == CharacterRole.Elite ? 4 : 1;
            SpawnOrb(target.Position, target.role == CharacterRole.Enemy ? 1 : 3);
            if (!target.IsBoss) return true;
            BossesDefeated++; Boss = null; NextBossTime = Elapsed + settings.bossInterval;
            foreach (var sword in Projectiles) { sword.gameObject.SetActive(false); Destroy(sword.gameObject); }
            Projectiles.Clear();
            // Queue the reward to avoid moving remaining blades during the collision pass.
            for (int i = 0; i < settings.bossBladeReward; i++) reforges.Add(Elapsed);
            player.health = player.maximumHealth;
            SetNotice("英雄王已击败 · 回满生命 / 补充剑 / 获得经验", 6);
            return true;
        }
        public SwordWave SpawnWave(Vector2 origin, Vector2 direction, float skillMultiplier, float scale = 1)
            => CreateWave(settings.swordWavePrefab, origin, direction, skillMultiplier, scale);
        SwordWave CreateWave(SwordWave prefab, Vector2 origin, Vector2 direction, float skillMultiplier, float scale)
        {
            if (!prefab) return null;
            if (direction.sqrMagnitude < .01f) direction = player.Facing;
            var wave = Instantiate(prefab, origin, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90), projectilesRoot);
            wave.direction = direction.normalized; wave.skillMultiplier = skillMultiplier;
            wave.transform.localScale *= scale; Waves.Add(wave); PlaySound(SoundCue.Wave); return wave;
        }
        void Burst(Vector2 position, Color color, float scale = 1)
        {
            if (!settings.impactPrefab) return;
            var burst = Instantiate(settings.impactPrefab, position, Quaternion.identity, projectilesRoot);
            burst.transform.localScale *= scale;
            foreach (var spark in burst.sparks) spark.color = color;
            bursts.Add(burst);
        }
        static bool SweptHit(Vector2 from, Vector2 to, Vector2 point, float radius)
        {
            Vector2 delta = to - from;
            float t = delta.sqrMagnitude > .000001f ? Mathf.Clamp01(Vector2.Dot(point - from, delta) / delta.sqrMagnitude) : 0;
            return (from + t * delta - point).sqrMagnitude <= radius * radius;
        }
        void UpdateBuildEffects(float dt)
        {
            if (!player.alive) return;
            while (shatters.Count > 0)
            {
                var effect = shatters.Dequeue();
                Burst(effect.position, new Color(.4f, 1, .9f), Has(UpgradeId.ShatterBurst) ? 3 : 1);
                foreach (var enemy in Enemies)
                {
                    if (!enemy.alive) continue;
                    float distance = Vector2.Distance(enemy.Position, effect.position);
                    if (Has(UpgradeId.ShatterBurst) && distance <= settings.burstRadius + enemy.bodyHitRadius)
                        Damage(enemy, ResolvePlayerDamage(enemy, Value(UpgradeId.ShatterBurst), false, Random.value));
                    if (Has(UpgradeId.Knockback) && enemy.alive && distance <= settings.knockbackRadius + enemy.bodyHitRadius)
                    {
                        Vector2 direction = enemy.Position - effect.position;
                        if (direction.sqrMagnitude < .001f) direction = effect.direction;
                        enemy.transform.position = Vector2.ClampMagnitude(enemy.Position + direction.normalized * SpatialValue(UpgradeId.Knockback), settings.arenaRadius - enemy.bodyHitRadius);
                        enemy.PlaceBlades();
                    }
                }
            }
            int readyBlades = 0;
            for (int i = reforges.Count - 1; i >= 0; i--)
                if (reforges[i] <= Elapsed) { readyBlades++; reforges.RemoveAt(i); }
            if (readyBlades > 0) player.AddBlades(readyBlades, MaxBladeCount);
            for (int i = Waves.Count - 1; i >= 0; i--)
            {
                var wave = Waves[i];
                if (wave.homing && (!wave.target || !wave.target.alive)) wave.target = NearestEnemy(wave.transform.position);
                Vector2 previous = wave.transform.position;
                wave.Step(Mathf.Min(dt, Mathf.Max(0, wave.lifetime - wave.age)));
                foreach (var enemy in Enemies)
                    if (enemy.alive && (!wave.homing || enemy == wave.target) && SweptHit(previous, wave.transform.position, enemy.Position, wave.CollisionRadius + enemy.bodyHitRadius) && wave.HitTargets.Add(enemy.GetInstanceID()))
                    {
                        Damage(enemy, ResolvePlayerDamage(enemy, wave.skillMultiplier, false, Random.value, wave.canCrit));
                        Burst(enemy.Position, wave.artwork.color);
                        if (wave.singleHit) { wave.age = wave.lifetime; break; }
                    }
                if (wave.age >= wave.lifetime) { wave.gameObject.SetActive(false); Destroy(wave.gameObject); Waves.RemoveAt(i); }
            }
            for (int i = bursts.Count - 1; i >= 0; i--)
                if (bursts[i].Step(dt)) { Destroy(bursts[i].gameObject); bursts.RemoveAt(i); }
            UpdateFormation();
        }
    }
}
