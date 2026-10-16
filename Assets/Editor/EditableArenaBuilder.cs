using System.IO;
using System.Linq;
using BladeArena;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EditableArenaBuilder
{
    public const string ScenePath = "Assets/Scenes/BladeArena.unity";
    static readonly Color Mint = new Color(.4f, 1, .82f), Gold = new Color(1, .8f, .34f);
    static Font font;
    static Sprite dot, square;

    [MenuItem("Tools/Blade Arena/打开可编辑场景")]
    public static void Open()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
        else Build();
    }

    // Explicit command only. Never runs on import or overwrites an existing scene/prefab.
    [MenuItem("Tools/Blade Arena/创建缺少的初始资源")]
    public static void Build()
    {
        if (File.Exists(ScenePath)) { Debug.Log("BladeArena 已存在，保留你的编辑。使用“打开可编辑场景”进入。"); return; }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (string folder in new[] { "Assets/Art/UI", "Assets/Prefabs/Characters", "Assets/Prefabs/Weapons", "Assets/Prefabs/Pickups", "Assets/Prefabs/Environment", "Assets/Prefabs/UI", "Assets/Settings", "Assets/Scenes" }) Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        dot = Primitive("Dot", true); square = Primitive("Square", false);
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/NotoSansCJKsc-Regular.otf");
        if (!font) throw new System.InvalidOperationException("请先导入 Assets/Art/Fonts/NotoSansCJKsc-Regular.otf");
        var settings = AssetDatabase.LoadAssetAtPath<ArenaSettings>("Assets/Settings/DefaultArena.asset");
        if (!settings)
        {
            settings = ScriptableObject.CreateInstance<ArenaSettings>();
            settings.upgrades = AssetDatabase.FindAssets("t:UpgradeDefinition", new[] { "Assets/Settings/Upgrades" })
                .Select(id => AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(AssetDatabase.GUIDToAssetPath(id))).ToArray();
            settings.swordWavePrefab = AssetDatabase.LoadAssetAtPath<SwordWave>("Assets/Prefabs/Weapons/SwordWave.prefab");
            AssetDatabase.CreateAsset(settings, "Assets/Settings/DefaultArena.asset");
        }
        var blade = MakeBlade();
        var player = MakeCharacter("Artoria", CharacterRole.Player, "Artoria", blade, 100, 5, 3, 1.45f, 145, 1.55f, Mint);
        var enemy = MakeCharacter("Schoolgirl", CharacterRole.Enemy, "Schoolgirl", blade, 30, 1.65f, 2, 1.45f, -100, 1.55f, new Color(1, .35f, .35f));
        var elite = MakeCharacter("SchoolgirlElite", CharacterRole.Elite, "Schoolgirl", blade, 150, 2.3f, 7, 1.45f, -100, 1.7f, new Color(1, .5f, .8f));
        var boss = MakeCharacter("Gilgamesh", CharacterRole.Boss, "Gilgamesh", blade, 1200, 1.15f, 8, 1.95f, -90, 2.15f, Gold);
        var orb = MakeOrb(); var projectile = MakeProjectile(); var floor = MakeFloor(); var ui = MakeHud();
        var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraGo.tag = "MainCamera";
        var camera = cameraGo.GetComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 7;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .07f, .085f);
        camera.transform.position = new Vector3(0, 0, -10);
        var environment = new GameObject("Environment").transform;
        InstantiatePrefab(floor, environment, Vector2.zero);
        var boundary = new GameObject("Arena Boundary — radius 22").transform; boundary.SetParent(environment);
        for (int i = 0; i < 128; i++)
        {
            float a = i * Mathf.PI * 2 / 128;
            SpriteObject("Boundary marker", dot, boundary, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * settings.arenaRadius, Vector2.one * .12f, Mint * .7f, -10);
        }
        var actors = new GameObject("Actors").transform;
        var playerInstance = InstantiatePrefab(player.gameObject, actors, Vector2.zero).GetComponent<Combatant>();
        InstantiatePrefab(enemy.gameObject, actors, new Vector2(5.5f, 3));
        InstantiatePrefab(enemy.gameObject, actors, new Vector2(-5.5f, 4));
        var pickups = new GameObject("Pickups").transform;
        // Deterministic authored placement: changing this is a normal scene edit.
        for (int i = 0; i < 24; i++)
        {
            float a = i * 2.399963f, radius = 2.5f + (i % 5) * 3;
            var o = InstantiatePrefab(orb.gameObject, pickups, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius).GetComponent<PickupOrb>();
            o.value = i % 3 + 1; o.RefreshLabel();
            PrefabUtility.RecordPrefabInstancePropertyModifications(o);
            PrefabUtility.RecordPrefabInstancePropertyModifications(o.valueLabel);
        }
        var spawns = new GameObject("Spawn Points — move these to control spawning").transform;
        var points = new Transform[8];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new GameObject("Spawn " + (i + 1)).transform; points[i].SetParent(spawns);
            float a = i * Mathf.PI / 4; points[i].position = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 12;
        }
        var hud = InstantiatePrefab(ui.gameObject, null, Vector2.zero).GetComponent<GameHud>();
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var game = new GameObject("Game Manager").AddComponent<BladeGameController>();
        game.settings = settings; game.player = playerInstance; game.gameCamera = camera; game.hud = hud;
        game.actorsRoot = actors; game.pickupsRoot = pickups; game.projectilesRoot = new GameObject("Projectiles").transform;
        game.enemyPrefab = enemy; game.elitePrefab = elite; game.bossPrefab = boss;
        game.orbPrefab = orb; game.projectilePrefab = projectile; game.spawnPoints = points;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("EDITABLE_ARENA_BUILT: " + ScenePath);
    }

    static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/" + name + ".png");
    static Sprite Primitive(string name, bool circle)
    {
        string path = "Assets/Art/UI/" + name + ".png";
        if (!File.Exists(path))
        {
            int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, !circle || Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * size / 2) < size / 2 - 1 ? Color.white : Color.clear);
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 32; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static SpriteRenderer SpriteObject(string name, Sprite sprite, Transform parent, Vector2 position, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = color; sr.sortingOrder = order;
        go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
        return sr;
    }
    static GameObject Save(GameObject go, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path); Object.DestroyImmediate(go); return prefab;
    }
    static GameObject InstantiatePrefab(GameObject prefab, Transform parent, Vector2 pos)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false); go.transform.localPosition = pos;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform); return go;
    }
    static OrbitBlade MakeBlade()
    {
        const string path = "Assets/Prefabs/Weapons/OrbitBlade.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<OrbitBlade>(path); if (existing) return existing;
        var go = new GameObject("OrbitBlade"); var b = go.AddComponent<OrbitBlade>();
        var art = Art("Excalibur"); float width = .85f * art.bounds.size.x / art.bounds.size.y;
        b.artwork = SpriteObject("Artwork", art, go.transform, Vector2.zero, new Vector2(width, .85f), Color.white, 5);
        return Save(go, path).GetComponent<OrbitBlade>();
    }
    static Combatant MakeCharacter(string name, CharacterRole role, string artName, OrbitBlade blade, int health, float speed, int knives, float radius, float spin, float height, Color tint)
    {
        string path = "Assets/Prefabs/Characters/" + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<Combatant>(path); if (existing) return existing;
        var go = new GameObject(name); var actor = go.AddComponent<Combatant>();
        actor.role = role; actor.maximumHealth = health; actor.moveSpeed = speed; actor.orbitRadius = radius;
        actor.rotationSpeed = spin; actor.initialBladeCount = knives; actor.bladePrefab = blade; actor.bladeTint = role == CharacterRole.Player ? Color.white : tint;
        SpriteObject("Shadow", dot, go.transform, new Vector2(0, -.35f), new Vector2(.85f, .3f), new Color(0, 0, 0, .4f), 0);
        SpriteObject("Team Marker", dot, go.transform, new Vector2(0, -.4f), new Vector2(.8f, .2f), tint * .7f, 1);
        var sprite = Art(artName);
        actor.body = SpriteObject("Artwork — replace this Sprite", sprite, go.transform, new Vector2(0, .18f), new Vector2(height * sprite.bounds.size.x / sprite.bounds.size.y, height), Color.white, 3);
        actor.weaponRoot = new GameObject("Weapons").transform; actor.weaponRoot.SetParent(go.transform, false);
        BuildInitialBlades(actor);
        return Save(go, path).GetComponent<Combatant>();
    }
    public static void BuildInitialBlades(Combatant actor)
    {
        for (int i = 0; i < actor.initialBladeCount; i++)
        {
            var b = InstantiatePrefab(actor.bladePrefab.gameObject, actor.weaponRoot, Vector2.zero).GetComponent<OrbitBlade>();
            b.angleOffset = i * 360f / Mathf.Max(1, actor.initialBladeCount);
            float a = b.angleOffset * Mathf.Deg2Rad;
            b.transform.localPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * actor.orbitRadius;
            b.transform.localRotation = Quaternion.Euler(0, 0, b.angleOffset - 35);
            b.artwork.color = actor.bladeTint;
            PrefabUtility.RecordPrefabInstancePropertyModifications(b);
            PrefabUtility.RecordPrefabInstancePropertyModifications(b.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(b.artwork);
        }
    }
    static PickupOrb MakeOrb()
    {
        const string path = "Assets/Prefabs/Pickups/BladeOrb.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<PickupOrb>(path); if (existing) return existing;
        var go = new GameObject("BladeOrb"); var orb = go.AddComponent<PickupOrb>();
        SpriteObject("Glow", dot, go.transform, Vector2.zero, Vector2.one * .6f, new Color(.4f, 1, .82f, .15f), 0);
        SpriteObject("Core", dot, go.transform, Vector2.zero, Vector2.one * .36f, Mint, 1);
        SpriteObject("Highlight", dot, go.transform, new Vector2(-.06f, .06f), Vector2.one * .10f, Color.white, 2);
        var label = new GameObject("Value Label"); label.transform.SetParent(go.transform, false); label.transform.localPosition = new Vector3(0, -.35f, 0);
        orb.valueLabel = label.AddComponent<TextMesh>(); orb.valueLabel.text = "+1"; orb.valueLabel.font = font;
        orb.valueLabel.fontSize = 32; orb.valueLabel.characterSize = .085f; orb.valueLabel.anchor = TextAnchor.MiddleCenter;
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material; label.GetComponent<MeshRenderer>().sortingOrder = 6;
        return Save(go, path).GetComponent<PickupOrb>();
    }
    static GateProjectile MakeProjectile()
    {
        const string path = "Assets/Prefabs/Weapons/GateProjectile.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GateProjectile>(path); if (existing) return existing;
        var go = new GameObject("GateProjectile"); var shot = go.AddComponent<GateProjectile>();
        var art = Art("Excalibur");
        SpriteObject("Golden Sword", art, go.transform, Vector2.zero, new Vector2(.85f * art.bounds.size.x / art.bounds.size.y, .85f), Gold, 7);
        shot.warning = SpriteObject("Warning Lane", square, go.transform, new Vector2(0, 6), new Vector2(.045f, 12), new Color(1, .75f, .2f, .3f), -1).transform;
        return Save(go, path).GetComponent<GateProjectile>();
    }
    static GameObject MakeFloor()
    {
        const string path = "Assets/Prefabs/Environment/Courtyard.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing) return existing;
        var go = new GameObject("Courtyard"); var tile = Art("Courtyard");
        for (int x = -6; x <= 6; x++) for (int y = -6; y <= 6; y++)
            SpriteObject("Tile " + x + "," + y, tile, go.transform, new Vector2(x * 4, y * 4), Vector2.one * 4, new Color(.65f, .7f, .8f), -20);
        return Save(go, path);
    }

    static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent, false);
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = new Vector2(.5f, .5f); rt.sizeDelta = size; rt.anchoredPosition = position; return rt;
    }
    static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 pos, Color color, bool blocks = false)
    {
        var image = Rect(name, parent, min, max, size, pos).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = blocks; return image;
    }
    static Text Text(string name, Transform parent, string value, int size, Color color, Vector2 min, Vector2 max, Vector2 dimensions, Vector2 pos, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var text = Rect(name, parent, min, max, dimensions, pos).gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = size; text.text = value; text.color = color; text.alignment = align; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow; return text;
    }
    static Button Button(string name, Transform parent, string label, Vector2 dimensions, Vector2 pos, out Text text)
    {
        var background = Panel(name, parent, Vector2.one * .5f, Vector2.one * .5f, dimensions, pos, new Color(.09f, .24f, .25f), true);
        var button = background.gameObject.AddComponent<Button>(); button.targetGraphic = background;
        text = Text("Label", background.transform, label, 22, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter); return button;
    }
    static Image Bar(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size, Vector2 pos, Color color)
    {
        var background = Panel(name, parent, min, max, size, pos, new Color(.1f, .17f, .2f));
        var fill = Panel("Fill", background.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, color);
        fill.sprite = square; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = .35f; return fill;
    }
    static GameHud MakeHud()
    {
        const string path = "Assets/Prefabs/UI/GameHUD.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameHud>(path); if (existing) return existing;
        var go = new GameObject("Game HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 900); scaler.matchWidthOrHeight = 1;
        var hud = go.AddComponent<GameHud>();
        var header = Panel("Header", go.transform, new Vector2(0, 1), Vector2.one, new Vector2(0, 112), new Vector2(0, -56), new Color(.025f, .055f, .07f, .96f), true);
        Text("Title", header.transform, "剑之誓约 / SABER", 24, Mint, Vector2.zero, Vector2.one, new Vector2(-160, -76), new Vector2(-55, 34));
        hud.stats = Text("Stats", header.transform, "生命 100/100    剑 3/30    击杀 0", 20, Color.white, Vector2.zero, Vector2.one, new Vector2(-44, -76), new Vector2(0, 0));
        hud.timer = Text("Timer", header.transform, "LV 1     00:00     威胁 1", 16, new Color(.6f, .75f, .77f), Vector2.zero, Vector2.one, new Vector2(-44, -88), new Vector2(0, -32));
        hud.pauseButton = Button("Pause Button", header.transform, "暂停", new Vector2(88, 38), Vector2.zero, out hud.pauseLabel);
        var pauseRect = hud.pauseButton.GetComponent<RectTransform>(); pauseRect.anchorMin = pauseRect.anchorMax = Vector2.one; pauseRect.anchoredPosition = new Vector2(-64, -30);
        hud.experienceFill = Bar("Experience", go.transform, new Vector2(0, 1), Vector2.one, new Vector2(0, 5), new Vector2(0, -113), Mint);
        hud.notice = Text("Notice", go.transform, "拾取光球，壮大你的刀阵", 18, Gold, new Vector2(0, 1), Vector2.one, new Vector2(-30, 32), new Vector2(0, -140), TextAnchor.MiddleCenter);
        hud.bossLabel = Text("Boss Label", go.transform, "英雄王降临 01:00", 18, Gold, new Vector2(0, 1), Vector2.one, new Vector2(-30, 32), new Vector2(0, -177), TextAnchor.MiddleCenter);
        hud.bossFill = Bar("Boss Health", go.transform, new Vector2(.1f, 1), new Vector2(.9f, 1), new Vector2(0, 8), new Vector2(0, -200), Gold);
        hud.bossFill.transform.parent.gameObject.SetActive(false);
        Text("Controls", go.transform, "WASD / 方向键移动 · 拖动屏幕移动 · ESC 暂停", 16, new Color(.6f, .7f, .75f), Vector2.zero, Vector2.right, new Vector2(-20, 36), new Vector2(0, 25), TextAnchor.MiddleCenter);
        var joy = Panel("Joystick", go.transform, Vector2.one * .5f, Vector2.one * .5f, Vector2.one * 140, Vector2.zero, new Color(.4f, 1, .82f, .2f)); joy.sprite = dot;
        hud.joystick = joy.rectTransform;
        var knob = Panel("Knob", joy.transform, Vector2.one * .5f, Vector2.one * .5f, Vector2.one * 44, Vector2.zero, new Color(.4f, 1, .82f, .65f)); knob.sprite = dot; hud.joystickKnob = knob.rectTransform; joy.gameObject.SetActive(false);
        hud.pausePanel = Overlay("Pause Panel", go.transform);
        Text("Heading", hud.pausePanel.transform, "休整片刻", 38, Mint, Vector2.one * .5f, Vector2.one * .5f, new Vector2(400, 70), new Vector2(0, 110), TextAnchor.MiddleCenter);
        Text("Rules", hud.pausePanel.transform, "剑与敌刀相撞：双方碎裂\n剑命中敌人：造成直接伤害，剑保留\n敌刀正常命中后保留，被抵挡时消失", 22, Color.white, Vector2.one * .5f, Vector2.one * .5f, new Vector2(400, 110), Vector2.zero, TextAnchor.MiddleCenter);
        hud.resumeButton = Button("Resume", hud.pausePanel.transform, "继续战斗 [ESC]", new Vector2(380, 58), new Vector2(0, -135), out _);
        hud.deathPanel = Overlay("Death Panel", go.transform);
        Text("Heading", hud.deathPanel.transform, "刀阵破碎", 42, new Color(1, .4f, .4f), Vector2.one * .5f, Vector2.one * .5f, new Vector2(400, 70), new Vector2(0, 110), TextAnchor.MiddleCenter);
        hud.result = Text("Results", hud.deathPanel.transform, "生存 00:00    击杀 0", 23, Color.white, Vector2.one * .5f, Vector2.one * .5f, new Vector2(400, 110), Vector2.zero, TextAnchor.MiddleCenter);
        hud.restartButton = Button("Restart", hud.deathPanel.transform, "再来一局 [R]", new Vector2(380, 58), new Vector2(0, -135), out _);
        hud.upgradePanel = Overlay("Upgrade Panel", go.transform);
        Text("Heading", hud.upgradePanel.transform, "选择强化", 36, Mint, Vector2.one * .5f, Vector2.one * .5f, new Vector2(400, 60), new Vector2(0, 235), TextAnchor.MiddleCenter);
        Text("Hint", hud.upgradePanel.transform, "选择一项强化 · 按 1 / 2 / 3 或点击", 17, Color.white, Vector2.one * .5f, Vector2.one * .5f, new Vector2(400, 35), new Vector2(0, 180), TextAnchor.MiddleCenter);
        hud.upgradeButtons = new Button[3]; hud.upgradeTitles = new Text[3]; hud.upgradeDescriptions = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            hud.upgradeButtons[i] = Button("Choice " + (i + 1), hud.upgradePanel.transform, "", new Vector2(400, 96), new Vector2(0, 95 - 115 * i), out var unused);
            Object.DestroyImmediate(unused.gameObject);
            hud.upgradeTitles[i] = Text("Title", hud.upgradeButtons[i].transform, "强化 " + (i + 1), 24, Mint, Vector2.zero, Vector2.one, new Vector2(-36, -60), new Vector2(0, 22));
            hud.upgradeDescriptions[i] = Text("Description", hud.upgradeButtons[i].transform, "效果说明", 17, Color.white, Vector2.zero, Vector2.one, new Vector2(-36, -60), new Vector2(0, -22));
        }
        hud.pausePanel.SetActive(false); hud.deathPanel.SetActive(false); hud.upgradePanel.SetActive(false);
        return Save(go, path).GetComponent<GameHud>();
    }
    static GameObject Overlay(string name, Transform parent)
    {
        return Panel(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.01f, .025f, .04f, .94f), true).gameObject;
    }
}

[CustomEditor(typeof(Combatant))]
public sealed class CombatantInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var current = (Combatant)target;
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("当前生命", current.health + " / " + current.maximumHealth);
            EditorGUILayout.LabelField("当前持剑数 / 圈数", current.Blades.Count + " / " + current.RingCount);
            EditorGUILayout.LabelField("受击无敌剩余", current.invulnerability.ToString("0.00") + " 秒");
            Repaint();
        }
        EditorGUILayout.HelpBox("换图/大小：展开 Artwork 子物体。数值：在本组件修改。初始剑由 Weapons 子物体保存，可单独摆放。运行时不改写角色的原始外观尺寸。", MessageType.Info);
        if (!Application.isPlaying && GUILayout.Button("按 Initial Blade Count 更新初始剑阵"))
        {
            var actor = (Combatant)target;
            if (!actor.weaponRoot || !actor.bladePrefab) return;
            Undo.RegisterFullObjectHierarchyUndo(actor.gameObject, "Update initial blades");
            while (actor.weaponRoot.childCount > 0) Undo.DestroyObjectImmediate(actor.weaponRoot.GetChild(0).gameObject);
            EditableArenaBuilder.BuildInitialBlades(actor);
            foreach (Transform child in actor.weaponRoot) Undo.RegisterCreatedObjectUndo(child.gameObject, "Create initial blade");
            EditorUtility.SetDirty(actor);
            if (actor.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(actor.gameObject.scene);
        }
    }
}
