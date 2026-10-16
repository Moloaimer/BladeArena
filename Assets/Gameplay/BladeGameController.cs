using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace BladeArena
{
    public enum RunState { Playing, Paused, Upgrade, Dead, Attributes }
    public sealed partial class BladeGameController : MonoBehaviour
    {
        [Header("规则资源")]
        public ArenaSettings settings;
        [Header("场景引用")]
        public Camera gameCamera;
        public Combatant player;
        public Transform actorsRoot, pickupsRoot, projectilesRoot;
        public GameHud hud;
        [Tooltip("可拖动的出生点；太接近玩家时跳过。无安全点时在玩家附近安全距离生成。")]
        public Transform[] spawnPoints;
        [Header("生成所用 Prefab")]
        public Combatant enemyPrefab, elitePrefab, bossPrefab;
        public PickupOrb orbPrefab;
        public GateProjectile projectilePrefab;
        public RunState State { get; private set; } = RunState.Playing;
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public int Level { get; private set; } = 1;
        public int BossesDefeated { get; private set; }
        public float Experience { get; private set; }
        public float NextExperience { get; private set; }
        public float Best { get; private set; }
        public Combatant Boss { get; private set; }
        public float NextBossTime { get; private set; }
        public string Notice { get; private set; }
        public readonly List<Combatant> Enemies = new List<Combatant>();
        public readonly List<PickupOrb> Orbs = new List<PickupOrb>();
        public readonly List<GateProjectile> Projectiles = new List<GateProjectile>();
        public readonly int[] Choices = new int[3];
        float enemyTimer = 2, orbTimer, volleyTimer, pickupRadius, noticeTimer;
        Vector2 input, dragOrigin, dragCurrent;
        bool dragging, restarting;
        int touchId = -1;
        float cameraZ;
        RunState beforeAttributes;
        public bool Dragging => dragging;
        public Vector2 DragOrigin => dragOrigin;
        public Vector2 DragCurrent => dragCurrent;

        void Start()
        {
            if (!settings || !player || !gameCamera || !hud || !enemyPrefab || !elitePrefab || !bossPrefab || !orbPrefab || !projectilePrefab || !actorsRoot || !pickupsRoot || !projectilesRoot)
            { Debug.LogError("BladeGameController 缺少场景或 Prefab 引用。请在 Inspector 中补齐。", this); enabled = false; return; }
            cameraZ = gameCamera.transform.position.z;
            Best = PlayerPrefs.GetFloat("BladeSurvivor.Best", 0);
            NextExperience = settings.firstLevelExperience; pickupRadius = settings.pickupRadius;
            NextBossTime = settings.firstBossTime;
            InitializeProgression();
            InitializePresentation();
            foreach (var actor in actorsRoot.GetComponentsInChildren<Combatant>())
            {
                actor.Initialize(actor.IsPlayer ? MaxBladeCount : settings.enemyBladeLimit);
                if (actor != player)
                {
                    Enemies.Add(actor);
                    if (actor.IsBoss) Boss = actor;
                }
            }
            RecalculateStats();
            Orbs.AddRange(pickupsRoot.GetComponentsInChildren<PickupOrb>());
            hud.Bind(this);
            SetNotice("拾取光球，壮大你的剑阵", 5);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleAttributes();
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (State == RunState.Dead && Input.GetKeyDown(KeyCode.R)) RestartRun();
            if (State == RunState.Upgrade)
                for (int i = 0; i < 3; i++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) { ChooseUpgrade(i); break; }
            if (State != RunState.Playing) return;
            if (Input.GetKeyDown(KeyCode.E)) TryRefillSwords();
            if (Input.GetKeyDown(KeyCode.Q)) TryRoundTable();
            if (Input.GetKeyDown(KeyCode.R)) TryExcalibur();
            ReadInput();
            float dt = Mathf.Min(Time.deltaTime, .08f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / SimulationStep));
            for (int i = 0; i < steps && State == RunState.Playing; i++) Simulate(dt / steps);
            gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, new Vector3(player.Position.x, player.Position.y, cameraZ), 1 - Mathf.Exp(-9 * dt));
        }
        // Keep blade travel shorter than half its collision radius as stats grow.
        public float SimulationStep
        {
            get
            {
                float tipSpeed = player.moveSpeed + Mathf.Abs(player.rotationSpeed) * Mathf.Deg2Rad * Mathf.Max(3, player.OuterRadius) * (RoundTableActive ? 3 : 1);
                float radius = player.bladePrefab.hitRadius * player.BladeScale;
                return Mathf.Min(.006f, radius * .5f / Mathf.Max(1, tipSpeed));
            }
        }
        void ReadInput()
        {
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (Input.touchCount > 0)
            {
                foreach (var touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began && touchId < 0 && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject(touch.fingerId)))
                    { touchId = touch.fingerId; dragOrigin = touch.position; dragging = true; }
                    if (touch.fingerId != touchId) continue;
                    dragCurrent = touch.position;
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) ResetDrag();
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0) && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
                { dragging = true; dragOrigin = Input.mousePosition; }
                dragCurrent = Input.mousePosition;
                if (!Input.GetMouseButton(0)) ResetDrag();
            }
            if (dragging) input = Vector2.ClampMagnitude((dragCurrent - dragOrigin) / (Screen.height * .09f), 1);
            input = Vector2.ClampMagnitude(input, 1);
        }
        void ResetDrag() { dragging = false; touchId = -1; }
        public void TogglePause()
        {
            if (State == RunState.Attributes) { ToggleAttributes(); return; }
            if (State == RunState.Playing) State = RunState.Paused;
            else if (State == RunState.Paused) State = RunState.Playing;
            ResetDrag();
        }
        public void ToggleAttributes()
        {
            if (State == RunState.Attributes) State = beforeAttributes;
            else { beforeAttributes = State; State = RunState.Attributes; }
            ResetDrag();
        }
        public void RestartRun()
        {
            if (restarting) return;
            restarting = true;
            // Reload the authored scene, so all scene overrides and initial placements are restored.
            SceneManager.LoadScene(gameObject.scene.path);
        }
        public void SetNotice(string text, float duration = 3) { Notice = text; noticeTimer = duration; }

        public void Simulate(float dt)
        {
            if (State != RunState.Playing) return;
            Elapsed += dt; noticeTimer -= dt;
            if (noticeTimer <= 0) Notice = "";
            player.Step(IsCharging ? Vector2.zero : input, dt, Elapsed, settings.arenaRadius);
            foreach (var enemy in Enemies)
            {
                Vector2 direction = player.Position - enemy.Position;
                Vector2 strafe = new Vector2(-direction.y, direction.x).normalized * Mathf.Sin(Elapsed + enemy.GetInstanceID()) * .3f;
                enemy.Step((direction.normalized + strafe).normalized, dt, Elapsed, settings.arenaRadius);

            }
            UpdateActiveSkills(dt);
            PrepareBoss(dt);
            // Resolve every weapon clash before any body hit (including golden projectiles).
            foreach (var enemy in Enemies) if (enemy.alive) ResolveBladeClashes(enemy);
            UpdateProjectiles(dt);
            foreach (var enemy in Enemies)
            {
                if (!enemy.alive || !player.alive) continue;
                HitBody(player, enemy);
                if (enemy.alive && player.alive) HitBody(enemy, player);
            }
            UpdateBuildEffects(dt);
            for (int i = Enemies.Count - 1; i >= 0; i--)
                if (!Enemies[i].alive) { Destroy(Enemies[i].gameObject); Enemies.RemoveAt(i); }
            if (!player.alive) { EndRun(); return; }
            for (int i = Orbs.Count - 1; i >= 0; i--)
            {
                var orb = Orbs[i];
                if (Vector2.Distance(orb.transform.position, player.Position) < pickupRadius)
                    orb.transform.position = Vector2.MoveTowards(orb.transform.position, player.Position, dt * 9);
                if (Vector2.Distance(orb.transform.position, player.Position) < .55f)
                {
                    CollectOrb(orb.value);
                    orb.gameObject.SetActive(false); Destroy(orb.gameObject); Orbs.RemoveAt(i);
                }
            }
            enemyTimer -= dt; orbTimer -= dt;
            if (enemyTimer <= 0)
            {
                enemyTimer = Mathf.Max(settings.minimumSpawnInterval, settings.enemySpawnInterval - Elapsed / 70);
                if (Enemies.Count < settings.enemyLimit)
                {
                    bool elite = Elapsed >= settings.eliteStartTime && Random.value < settings.eliteChance;
                    var enemy = SpawnActor(elite ? elitePrefab : enemyPrefab, FindSpawnPosition());
                    enemy.moveSpeed += Mathf.Min(1.5f, Elapsed / 160);
                    enemy.AddBlades(Mathf.Min(16, (int)(Elapsed / 25)), settings.enemyBladeLimit);
                }
            }
            if (orbTimer <= 0)
            {
                orbTimer = settings.orbSpawnInterval;
                SpawnOrb(Vector2.ClampMagnitude(player.Position + Random.insideUnitCircle.normalized * Random.Range(3f, 11f), settings.arenaRadius - 1), Random.Range(1, 4));
            }
            if (Experience >= NextExperience) OfferUpgrade();
        }
        Vector2 FindSpawnPosition()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int start = Random.Range(0, spawnPoints.Length);
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    var point = spawnPoints[(start + i) % spawnPoints.Length];
                    if (point && point.position.magnitude < settings.arenaRadius - 1 && Vector2.Distance(point.position, player.Position) >= 7) return point.position;
                }
            }
            Vector2 inward = player.Position.sqrMagnitude > 1 ? -player.Position.normalized : Vector2.up;
            return Vector2.ClampMagnitude(player.Position + inward * 9, settings.arenaRadius - 1);
        }
        public Combatant SpawnActor(Combatant prefab, Vector2 position)
        {
            var actor = Instantiate(prefab, position, Quaternion.identity, actorsRoot);
            actor.maximumHealth = Mathf.Max(1, Mathf.RoundToInt(actor.maximumHealth * (1 + settings.enemyHealthGrowthPerMinute * Elapsed / 60)));
            actor.bladeDamage *= 1 + settings.enemyDamageGrowthPerMinute * Elapsed / 60;
            actor.Initialize(actor.IsPlayer ? MaxBladeCount : settings.enemyBladeLimit); Enemies.Add(actor); return actor;
        }
        public void SpawnOrb(Vector2 position, int value)
        {
            if (Orbs.Count >= settings.orbLimit) return;
            var orb = Instantiate(orbPrefab, position, Quaternion.identity, pickupsRoot);
            orb.value = value; orb.RefreshLabel(); Orbs.Add(orb);
        }
        public void SpawnBoss()
        {
            if (Boss) return;
            if (Enemies.Count >= settings.enemyLimit)
            { Destroy(Enemies[0].gameObject); Enemies.RemoveAt(0); }
            Boss = SpawnActor(bossPrefab, FindSpawnPosition());
            Boss.maximumHealth += BossesDefeated * settings.bossHealthGrowth; Boss.health = Boss.maximumHealth;
            Boss.AddBlades(BossesDefeated * 2, settings.enemyBladeLimit); volleyTimer = 3;
            SetNotice("英雄王 · 吉尔伽美什降临", 4); PlaySound(SoundCue.Boss);
        }
        void UpdateBoss(float dt)
        {
            PrepareBoss(dt);
            UpdateProjectiles(dt);
        }
        void PrepareBoss(float dt)
        {
            if (!player.alive) return;
            if (!Boss && Elapsed >= NextBossTime) SpawnBoss();
            if (Boss && Boss.alive)
            {
                volleyTimer -= dt;
                if (volleyTimer <= 0)
                {
                    bool enraged = Boss.health <= Boss.maximumHealth / 2;
                    volleyTimer = enraged ? settings.enragedVolleyInterval : settings.volleyInterval;
                    SpawnVolley(enraged ? 5 : 3); Boss.AddBlades(2, settings.enemyBladeLimit);
                    SetNotice("王之财宝！避开金色射线或用剑格挡", 2);
                }
            }
        }
        void UpdateProjectiles(float dt)
        {
            if (!player.alive) return;
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                var shot = Projectiles[i]; shot.Step(dt);
                if (!shot.Launched) continue;
                bool remove = shot.age > shot.warningDuration + shot.lifetime || shot.transform.position.magnitude > settings.arenaRadius + 2;
                for (int ring = 0; !remove && !player.SwordsHidden && ring < player.RingCount; ring++)
                    for (int j = player.Blades.Count - 1; !remove && j >= 0; j--)
                        if (player.Blades[j].Ring == ring && Overlaps(player.Blades[j].transform.position, shot.transform.position, player.Blades[j].CollisionRadius + shot.hitRadius))
                        { BlockEnemyBlade(player.Blades[j], shot.transform.position); remove = true; }
                bool touching = Overlaps(player.Position, shot.transform.position, player.bodyHitRadius + shot.hitRadius);
                if (!remove && touching && !shot.touchingPlayer) remove = !Damage(player, Boss ? Boss.bladeDamage : 10);
                shot.touchingPlayer = touching;
                if (remove) { shot.gameObject.SetActive(false); Destroy(shot.gameObject); Projectiles.Remove(shot); }
            }
        }
        public void SpawnVolley(int count)
        {
            if (!Boss || Projectiles.Count > 32) return;
            Vector2 aim = (player.Position - Boss.Position).normalized;
            if (aim.sqrMagnitude < .01f) aim = Vector2.down;
            Vector2 side = new Vector2(-aim.y, aim.x);
            for (int i = 0; i < count; i++)
            {
                float offset = i - (count - 1) * .5f;
                Vector2 origin = Boss.Position - aim * 1.4f + side * offset * .9f;
                Vector2 direction = (player.Position + side * offset * .5f - origin).normalized;
                var shot = Instantiate(projectilePrefab, origin, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90), projectilesRoot);
                shot.direction = direction; Projectiles.Add(shot);
            }
        }
        void EndRun()
        {
            State = RunState.Dead; ResetDrag();
            if (Elapsed > Best) { Best = Elapsed; PlayerPrefs.SetFloat("BladeSurvivor.Best", Best); PlayerPrefs.Save(); }
        }
    }
}
