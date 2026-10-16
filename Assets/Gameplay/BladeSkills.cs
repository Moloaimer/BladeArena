using UnityEngine;

namespace BladeArena
{
    public sealed partial class BladeGameController
    {
        public float Mana { get; private set; }
        public int ManaSlots => Has(UpgradeId.ManaReservoir) ? settings.manaCapacity : 0;
        public float RoundTableRemaining { get; private set; }
        public bool RoundTableActive => RoundTableRemaining > 0;
        public float ChargeRemaining { get; private set; }
        public bool IsCharging => ChargeRemaining > 0;
        public float HolyFlashRemaining { get; private set; }
        public float RoundTableDuration => Has(UpgradeId.RoundTable) ? Definition(UpgradeId.RoundTable).amount + Definition(UpgradeId.RoundTable).perExtraLevel * Mathf.Max(0, UpgradeLevel(UpgradeId.RoundTable) - 1) : 0;
        public float ExcaliburMultiplier => Has(UpgradeId.Excalibur) ? Definition(UpgradeId.Excalibur).amount + Mathf.Max(0, UpgradeLevel(UpgradeId.Excalibur) - 1) * Definition(UpgradeId.Excalibur).perExtraLevel : 0;
        public float ReflectionRate => Has(UpgradeId.Reflect) ? Definition(UpgradeId.Reflect).amount + (UpgradeLevel(UpgradeId.Reflect) - 1) * Definition(UpgradeId.Reflect).perExtraLevel : 0;
        public bool CanRefill => CanCast(1) && player.Blades.Count < MaxBladeCount;
        public bool CanRoundTable => Has(UpgradeId.RoundTable) && CanCast(2) && !RoundTableActive && player.Blades.Count > 0;
        public bool CanExcalibur => Has(UpgradeId.Excalibur) && CanCast(3);
        GameAudio sounds;
        SpellAura aura;
        void InitializePresentation()
        {
            if (settings.soundPrefab) sounds = Instantiate(settings.soundPrefab, transform);
            if (settings.spellAuraPrefab) { aura = Instantiate(settings.spellAuraPrefab, player.transform); aura.gameObject.SetActive(false); }
        }
        public void PlaySound(SoundCue cue) { if (sounds) sounds.Play(cue); }
        public void AddMana(float amount)
        {
            if (ManaSlots == 0 || amount <= 0) return;
            int old = Mathf.FloorToInt(Mana);
            Mana = Mathf.Min(ManaSlots, Mana + amount);
            if (Mathf.FloorToInt(Mana) > old) PlaySound(SoundCue.Mana);
        }
        bool CanCast(float cost) => State == RunState.Playing && player.alive && !IsCharging && ManaSlots > 0 && Mana + .00001f >= cost;
        public bool TryRefillSwords()
        {
            if (!CanCast(1) || player.Blades.Count >= MaxBladeCount) return false;
            Mana = Mathf.Max(0, Mana - 1);
            player.AddBlades(MaxBladeCount - player.Blades.Count, MaxBladeCount);
            PlaySound(SoundCue.Refill); SetNotice("魔力重整 · 剑阵已补满"); return true;
        }
        public bool TryRoundTable()
        {
            if (!Has(UpgradeId.RoundTable) || !CanCast(2) || RoundTableActive || player.Blades.Count == 0) return false;
            Mana = Mathf.Max(0, Mana - 2); RoundTableRemaining = RoundTableDuration;
            RecalculateStats(); PlaySound(SoundCue.RoundTable); SetNotice("圆桌剑阵 · 剑不会被撞碎"); return true;
        }
        public bool TryExcalibur()
        {
            if (!Has(UpgradeId.Excalibur) || !CanCast(3)) return false;
            Mana = Mathf.Max(0, Mana - 3); ChargeRemaining = settings.excaliburChargeDuration;
            player.HideSwords(true); ResetDrag(); input = Vector2.zero;
            PushEnemiesFromCharge();
            if (aura) { aura.gameObject.SetActive(true); aura.Show(0); }
            PlaySound(SoundCue.Charge); SetNotice("Excalibur · 蓄力中", settings.excaliburChargeDuration); return true;
        }
        void PushEnemiesFromCharge()
        {
            foreach (var enemy in Enemies)
            {
                if (!enemy || !enemy.alive) continue;
                Vector2 direction = enemy.Position - player.Position;
                float distance = settings.excaliburPushRadius + enemy.bodyHitRadius;
                if (direction.sqrMagnitude >= distance * distance) continue;
                if (direction.sqrMagnitude < .001f) direction = Vector2.up;
                enemy.transform.position = Vector2.ClampMagnitude(player.Position + direction.normalized * distance, settings.arenaRadius - enemy.bodyHitRadius);
                enemy.PlaceBlades();
            }
        }
        public bool InSpellViewport(Combatant enemy)
        {
            Vector3 view = gameCamera.WorldToViewportPoint(enemy.Position);
            float ry = enemy.bodyHitRadius / (2 * gameCamera.orthographicSize), rx = ry / gameCamera.aspect;
            return view.z > 0 && view.x >= -rx && view.x <= 1 + rx && view.y >= -ry && view.y <= 1 + ry;
        }
        void UpdateActiveSkills(float dt)
        {
            if (!player.alive) return;
            AddMana(settings.manaPerSecond * dt);
            HolyFlashRemaining = Mathf.Max(0, HolyFlashRemaining - dt);
            if (RoundTableActive)
            {
                RoundTableRemaining = Mathf.Max(0, RoundTableRemaining - dt);
                if (!RoundTableActive) RecalculateStats();
            }
            if (!IsCharging) return;
            PushEnemiesFromCharge();
            ChargeRemaining = Mathf.Max(0, ChargeRemaining - dt);
            if (aura) aura.Show(1 - ChargeRemaining / settings.excaliburChargeDuration);
            if (IsCharging) return;
            player.HideSwords(false);
            if (aura) aura.gameObject.SetActive(false);
            HolyFlashRemaining = .3f;
            foreach (var enemy in Enemies)
                if (enemy && enemy.alive && InSpellViewport(enemy))
                { Damage(enemy, ResolvePlayerDamage(enemy, ExcaliburMultiplier, false, Random.value)); Burst(enemy.Position, new Color(1, .9f, .35f), 2); }
            PlaySound(SoundCue.Holy); SetNotice("Excalibur · 圣剑解放", 2);
        }
        void CancelActiveSkills()
        {
            ChargeRemaining = 0; RoundTableRemaining = 0;
            player.HideSwords(false);
            if (aura) aura.gameObject.SetActive(false);
        }
    }
}
