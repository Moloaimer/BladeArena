using System.Collections.Generic;
using UnityEngine;

namespace BladeArena
{
    public enum CharacterRole { Player, Enemy, Elite, Boss }
    public sealed class Combatant : MonoBehaviour
    {
        [Header("角色与外观（直接修改子物体 SpriteRenderer）")]
        public CharacterRole role;
        public SpriteRenderer body;
        public Transform weaponRoot;
        public OrbitBlade bladePrefab;
        public Color bladeTint = Color.white;
        [Header("基础数值")]
        [Min(1)] public int maximumHealth = 100;
        [InspectorName("攻击力"), Min(1)] public float bladeDamage = 10;
        [Min(0)] public float moveSpeed = 5;
        [Min(.1f)] public float orbitRadius = 1.45f;
        public float rotationSpeed = 145;
        [Min(.05f)] public float bodyHitRadius = .4f;
        [Tooltip("编辑器预览持剑数。改完用组件右键更新初始剑阵；运行时新增剑使用 Blade Prefab。")]
        [Min(0)] public int initialBladeCount = 3;
        [Header("受击反馈（不提供无敌，也不击退）")]
        public Color hitTint = new Color(1, .18f, .18f);
        public float hitFlashDuration = .12f;
        [System.NonSerialized] public int health;
        [System.NonSerialized] public float invulnerability, angle;
        [System.NonSerialized] public bool alive = true;
        public readonly List<OrbitBlade> Blades = new List<OrbitBlade>();
        public int RingCount { get; set; } = 1;
        public bool DefensiveInnerRing { get; set; }
        public bool ChaoticFormation { get; set; }
        public bool SwordsHidden { get; private set; }
        public float HitFlashRemaining { get; private set; }
        public float BladeScale { get; set; } = 1;
        public Vector2 Facing { get; private set; } = Vector2.up;
        public float OuterRadius => orbitRadius * (RingCount == 3 ? 1.3f : RingCount == 2 ? 1.15f : 1) * (ChaoticFormation ? 1.09f : 1);
        Vector3 restPosition;
        Quaternion restRotation;
        Color restColor;
        bool layoutDirty;
        public bool IsPlayer => role == CharacterRole.Player;
        public bool IsBoss => role == CharacterRole.Boss;
        public Vector2 Position => transform.position;

        public void Initialize(int limit)
        {
            health = maximumHealth; alive = true; invulnerability = 0;
            restPosition = body.transform.localPosition;
            restRotation = body.transform.localRotation;
            restColor = body.color;
            Blades.Clear();
            Blades.AddRange(weaponRoot.GetComponentsInChildren<OrbitBlade>());
            while (Blades.Count > limit) BreakBlade(Blades.Count - 1);
            if (Blades.Count == 0) AddBlades(initialBladeCount, limit);
            Rearrange();
        }
        public void AddBlades(int count, int limit)
        {
            for (int n = 0; n < count && Blades.Count < limit; n++)
            {
                var blade = Instantiate(bladePrefab, weaponRoot);
                blade.name = "Orbit Blade";
                blade.artwork.color = bladeTint; Blades.Add(blade);
            }
            Rearrange();
        }
        public void BreakBlade(int index)
        {
            var blade = Blades[index];
            Blades.RemoveAt(index);
            blade.gameObject.SetActive(false);
            Destroy(blade.gameObject);
            layoutDirty = true;
        }
        public void Rearrange()
        {
            layoutDirty = false;
            int rings = Mathf.Max(1, RingCount);
            for (int ring = 0; ring < rings; ring++)
            {
                int distributed = rings == 3 ? Mathf.Min(30, Blades.Count) : Blades.Count;
                int count = (distributed + rings - 1 - ring) / rings;
                if (rings == 3 && ring == 2) count += Mathf.Max(0, Blades.Count - 30);
                for (int j = 0; j < count; j++)
                {
                    int index = rings == 3 && ring == 2 && j >= 10 ? 30 + j - 10 : ring + j * rings;
                    var blade = Blades[index];
                    blade.Ring = ring; blade.angleOffset = 360f * j / count;
                }
            }
            PlaceBlades();
        }
        public void Step(Vector2 input, float dt, float time, float arenaRadius)
        {
            if (layoutDirty) Rearrange();
            transform.position = Vector2.ClampMagnitude(Position + input * moveSpeed * dt, arenaRadius - bodyHitRadius);
            angle = (angle + rotationSpeed * dt) % 360;
            if (input.sqrMagnitude > .01f) Facing = input.normalized;
            invulnerability = Mathf.Max(0, invulnerability - dt);
            HitFlashRemaining = Mathf.Max(0, HitFlashRemaining - dt);
            body.color = HitFlashRemaining > 0 ? hitTint : restColor;
            body.enabled = invulnerability <= 0 || (int)(invulnerability * 15) % 2 == 0;
            float walking = input.sqrMagnitude > .01f ? 1 : 0;
            body.transform.localPosition = restPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(time * 12)) * .065f * walking);
            body.transform.localRotation = restRotation * Quaternion.Euler(0, 0, Mathf.Sin(time * 12) * 3 * walking);
            if (Mathf.Abs(input.x) > .15f) body.flipX = input.x < 0;
            PlaceBlades();
        }
        public void PlaceBlades()
        {
            for (int i = 0; i < Blades.Count; i++)
            {
                var blade = Blades[i]; blade.SetSize(BladeScale);
                float a = angle * (blade.Ring % 2 == 0 ? 1 : -1) + blade.angleOffset;
                float radius = orbitRadius * (RingCount == 1 ? 1 : RingCount == 2 ? (blade.Ring == 0 ? .72f : 1.15f) : (.65f + .325f * blade.Ring));
                if (ChaoticFormation)
                {
                    a += Mathf.Sin(angle * Mathf.Deg2Rad * 1.7f + i * 2.4f) * 28;
                    radius *= 1 + .09f * Mathf.Sin(angle * Mathf.Deg2Rad * 2.1f + i * 1.7f);
                }
                blade.transform.localPosition = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * radius;
                blade.transform.localRotation = Quaternion.Euler(0, 0, a - 35);

            }
        }
        public void FlashHit()
        {
            HitFlashRemaining = hitFlashDuration;
            body.color = hitTint;
        }
        public void HideSwords(bool hidden)
        {
            SwordsHidden = hidden; weaponRoot.gameObject.SetActive(!hidden);
            foreach (var sword in Blades) sword.BodyContacts.Clear();
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = IsPlayer ? Color.cyan : Color.red;
            Gizmos.DrawWireSphere(transform.position, bodyHitRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, orbitRadius);
        }
    }
}
