using System.IO;
using System.Linq;
using BladeArena;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit authoring tool. It never runs automatically or rebuilds UI in Play mode.
public static class PixelPresentationBuilder
{
    static readonly Color Ink = new Color(.035f, .055f, .105f), PanelColor = new Color(.065f, .095f, .16f);
    static readonly Color Edge = new Color(.24f, .38f, .49f), Mint = new Color(.45f, .95f, .85f), Gold = new Color(1, .81f, .4f);
    static Font font;
    static Sprite square;
    [MenuItem("Tools/Blade Arena/应用像素界面与特效模板（覆盖布局）")]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorUtility.DisplayDialog("应用界面模板", "这会重置 GameHUD 布局和远程特效 Prefab。手动编辑过时请先备份。", "应用", "取消")) return;
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/NotoSansCJKsc-Regular.otf");
        square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Square.png");
        MakeSpellAssets(); StyleHud(); AssetDatabase.SaveAssets(); ActiveRulesBuilder.Apply();
    }
    static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false); rt.anchorMin = rt.anchorMax = Vector2.one * .5f;
        rt.sizeDelta = size; rt.anchoredPosition = position; return rt;
    }
    static Image Box(string name, Transform parent, Vector2 size, Vector2 position, Color color, bool blocks = false)
    {
        var image = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = blocks; return image;
    }
    static Text Label(string name, Transform parent, string value, Vector2 size, Vector2 position, int fontSize = 20, Color? color = null)
    {
        var text = Rect(name, parent, size, position).gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = fontSize; text.color = color ?? Color.white;
        text.text = value; text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = size.y <= fontSize * 2 ? VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;
        return text;
    }
    static void Place(Component c, Vector2 size, Vector2 position, Vector2? anchor = null)
    {
        var rt = c.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = anchor ?? Vector2.one * .5f;
        rt.sizeDelta = size; rt.anchoredPosition = position;
    }
    static void Border(Transform parent, Color color)
    {
        if (parent.Find("Pixel Frame")) Object.DestroyImmediate(parent.Find("Pixel Frame").gameObject);
        var frame = Rect("Pixel Frame", parent, Vector2.zero, Vector2.zero);
        frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one;
        for (int i = 0; i < 4; i++)
        {
            var strip = Box("Edge " + i, frame, Vector2.zero, Vector2.zero, color).rectTransform;
            if (i < 2) { strip.anchorMin = new Vector2(0, i); strip.anchorMax = new Vector2(1, i); strip.sizeDelta = new Vector2(-8, 2); }
            else { strip.anchorMin = new Vector2(i - 2, 0); strip.anchorMax = new Vector2(i - 2, 1); strip.sizeDelta = new Vector2(2, -8); }
        }
    }
    static Button Button(string name, Transform parent, string title, Vector2 size, Vector2 position)
    {
        var image = Box(name, parent, size, position, PanelColor, true);
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(.6f, .85f, 1); colors.pressedColor = Mint; button.colors = colors;
        var label = Label("Label", image.transform, title, size - new Vector2(16, 4), Vector2.zero, 19);
        label.alignment = TextAnchor.MiddleCenter; Border(image.transform, Edge); return button;
    }
    static Image Bar(string name, Transform parent, Vector2 size, Vector2 pos, Color color)
    {
        var back = Box(name, parent, size, pos, Ink);
        Border(back.transform, Edge);
        var fill = Box("Fill", back.transform, size - Vector2.one * 6, Vector2.zero, color);
        fill.sprite = square; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 1;
        return fill;
    }
    static void StyleHud()
    {
        const string path = "Assets/Prefabs/UI/GameHUD.prefab";
        var root = PrefabUtility.LoadPrefabContents(path); var hud = root.GetComponent<GameHud>();
        root.GetComponent<Canvas>().pixelPerfect = true;
        foreach (var label in root.GetComponentsInChildren<Text>(true)) { label.font = font; label.verticalOverflow = VerticalWrapMode.Truncate; }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        { button.image.color = PanelColor; Border(button.transform, Edge); }
        var header = root.transform.Find("Header"); header.GetComponent<Image>().color = Ink;
        var title = header.Find("Title").GetComponent<Text>(); title.text = "SABER / 剑之誓约"; title.fontSize = 24; title.color = Gold;
        title.verticalOverflow = VerticalWrapMode.Overflow;
        Place(title, new Vector2(380, 34), new Vector2(216, -26), new Vector2(0, 1));
        Place(hud.stats, new Vector2(510, 30), new Vector2(281, -58), new Vector2(0, 1)); hud.stats.fontSize = 20;
        hud.stats.text = "生命 100/100    剑 3/30";
        Place(hud.timer, new Vector2(480, 26), new Vector2(265, -90), new Vector2(0, 1)); hud.timer.fontSize = 17; hud.timer.color = Mint;
        hud.timer.text = "LV 1   /   00:00   /   击杀 0";
        Place(hud.pauseButton, new Vector2(88, 36), new Vector2(-66, -28), Vector2.one); hud.pauseLabel.fontSize = 19;
        foreach (string name in new[] { "Attributes Panel", "Attribute Button", "Vitals", "Combat Readout" })
        { var old = root.transform.Find(name); if (old) Object.DestroyImmediate(old.gameObject); }
        var vitals = Rect("Vitals", root.transform, new Vector2(330, 8), new Vector2(-190, -54)); vitals.anchorMin = vitals.anchorMax = Vector2.one;
        hud.healthFill = Bar("Health", vitals, new Vector2(330, 12), Vector2.zero, Mint);
        hud.combatStats = Label("Combat Readout", root.transform, "攻击力 10   暴击率 0%   护盾 0", new Vector2(330, 28), new Vector2(-190, -82), 17, Mint);
        hud.combatStats.rectTransform.anchorMin = hud.combatStats.rectTransform.anchorMax = Vector2.one;
        hud.attributeButton = Button("Attribute Button", root.transform, "属性 [Tab]", new Vector2(138, 36), new Vector2(-193, -28));
        hud.attributeButton.GetComponent<RectTransform>().anchorMin = hud.attributeButton.GetComponent<RectTransform>().anchorMax = Vector2.one;
        var controls = root.transform.Find("Controls").GetComponent<Text>(); controls.text = "WASD 移动   /   拖动移动   /   TAB 属性   /   ESC 暂停";
        controls.color = new Color(.74f, .82f, .86f); controls.fontSize = 16;
        for (int i = 0; i < 3; i++)
        {
            Place(hud.upgradeButtons[i], new Vector2(620, 140), new Vector2(0, 138 - i * 160));
            Place(hud.upgradeTitles[i], new Vector2(572, 38), new Vector2(0, 43)); hud.upgradeTitles[i].fontSize = 23;
            Place(hud.upgradeDescriptions[i], new Vector2(572, 88), new Vector2(0, -17)); hud.upgradeDescriptions[i].fontSize = 17;
        }
        Place(hud.upgradePanel.transform.Find("Heading"), new Vector2(620, 60), new Vector2(0, 294));
        Place(hud.upgradePanel.transform.Find("Hint"), new Vector2(620, 36), new Vector2(0, 243));
        foreach (var overlay in new[] { hud.pausePanel, hud.upgradePanel, hud.deathPanel }) overlay.GetComponent<Image>().color = new Color(.015f, .025f, .065f, .97f);
        var overlayImage = Box("Attributes Panel", root.transform, Vector2.zero, Vector2.zero, new Color(.01f, .02f, .045f, .96f), true);
        overlayImage.rectTransform.anchorMin = Vector2.zero; overlayImage.rectTransform.anchorMax = Vector2.one;
        hud.attributePanel = overlayImage.gameObject;
        var panel = Box("Character Sheet", overlayImage.transform, new Vector2(1024, 780), Vector2.zero, Ink, true); Border(panel.transform, Gold);
        Label("Title", panel.transform, "剑之档案  /  CHARACTER", new Vector2(740, 48), new Vector2(-100, 345), 30, Gold);
        hud.closeAttributes = Button("Close", panel.transform, "返回 [Tab]", new Vector2(150, 38), new Vector2(405, 345));
        var left = Box("Current Attributes", panel.transform, new Vector2(310, 646), new Vector2(-335, -25), PanelColor); Border(left.transform, Edge);
        Label("Heading", left.transform, "当前属性", new Vector2(270, 36), new Vector2(0, 285), 22, Mint);
        hud.attributeStats = Label("Values", left.transform, "", new Vector2(274, 446), new Vector2(0, 34), 16); hud.attributeStats.lineSpacing = .95f;
        hud.attributeResources = Label("Resources", left.transform, "", new Vector2(274, 106), new Vector2(0, -244), 17, Gold);
        var right = Rect("Owned Upgrades", panel.transform, new Vector2(640, 646), new Vector2(160, -25));
        hud.ownedCount = Label("Count", right, "已获强化 0 / 27", new Vector2(620, 60), new Vector2(0, 285), 20, Mint);
        hud.ownedSlots = new OwnedUpgradeSlot[27];
        for (int i = 0; i < 27; i++)
        {
            var button = Button("Upgrade Slot " + (i + 1), right, "强化 Lv.1", new Vector2(304, 32), new Vector2(i % 2 == 0 ? -158 : 158, 230 - i / 2 * 32));
            var slot = button.gameObject.AddComponent<OwnedUpgradeSlot>();
            slot.label = button.GetComponentInChildren<Text>(); slot.label.fontSize = 17;
            slot.accent = Box("Rarity", button.transform, new Vector2(4, 20), new Vector2(-141, 0), Mint);
            hud.ownedSlots[i] = slot;
        }
        var tooltip = Box("Hover Details", right, new Vector2(622, 136), new Vector2(0, -279), PanelColor); Border(tooltip.transform, Edge);
        hud.tooltipTitle = Label("Title", tooltip.transform, "强化详情", new Vector2(592, 28), new Vector2(0, 49), 20, Gold);
        hud.tooltipBody = Label("Body", tooltip.transform, "鼠标悬停已获强化，查看当前效果。", new Vector2(592, 96), new Vector2(0, -13), 16);
        Label("Footer", panel.transform, "查看时战斗暂停 · 悬停查看效果 · ESC / TAB 返回", new Vector2(940, 28), new Vector2(0, -366), 16, new Color(.6f, .7f, .8f));
        hud.attributePanel.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
    }
    static Sprite PixelSprite(string name, int width, int height, System.Func<int, int, Color> pixel)
    {
        const string folder = "Assets/Art/Effects"; Directory.CreateDirectory(folder);
        string path = folder + "/" + name + ".png";
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) texture.SetPixel(x, y, pixel(x, y));
        texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, int order = 8)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position;
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
        go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1); return renderer;
    }
    static PixelSpellVisual Trails(Transform root, Sprite spark, Color tint, float scale)
    {
        var old = root.Find("Pixel Effects"); if (old) Object.DestroyImmediate(old.gameObject);
        var fx = new GameObject("Pixel Effects"); fx.transform.SetParent(root, false);
        var visual = fx.AddComponent<PixelSpellVisual>(); visual.trailPixels = new SpriteRenderer[7];
        for (int i = 0; i < 7; i++)
            visual.trailPixels[i] = Sprite("Afterimage " + i, fx.transform, spark, new Vector2((i % 2 == 0 ? 1 : -1) * .12f * scale, (-.4f - i * .16f) * scale), new Vector2(.10f, .26f) * scale, tint);
        return visual;
    }
    static SwordWave Wave(string name, Sprite art, Sprite spark, Color color, Vector2 size, float radius, float speed, float life, bool twin = false)
    {
        string path = "Assets/Prefabs/Weapons/" + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var go = existing ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
        var wave = go.GetComponent<SwordWave>() ?? go.AddComponent<SwordWave>();
        if (wave.artwork) Object.DestroyImmediate(wave.artwork.gameObject);
        wave.artwork = Sprite("Pixel Blade", go.transform, art, Vector2.zero, size, color);
        wave.speed = speed; wave.lifetime = life; wave.hitRadius = radius; wave.homing = wave.singleHit = twin;
        wave.visual = Trails(go.transform, spark, color, twin ? .75f : Mathf.Min(1.2f, size.x * 2));
        if (size.y > 3) wave.visual.transform.localPosition = Vector3.down * size.y * .35f;
        PrefabUtility.SaveAsPrefabAsset(go, path);
        if (existing) PrefabUtility.UnloadPrefabContents(go); else Object.DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<SwordWave>(path);
    }
    static void MakeSpellAssets()
    {
        var beam = PixelSprite("SwordBeam", 16, 48, (x, y) =>
        {
            int d = Mathf.Abs(x * 2 - 15), extent = y > 32 ? (48 - y) / 2 : y < 10 ? 2 + y / 2 : 7;
            if (d > extent || y < 2) return Color.clear;
            return d < 3 ? Color.white : d < 5 ? new Color(.8f, .8f, .8f) : new Color(.45f, .45f, .45f);
        });
        var spark = PixelSprite("SpellSpark", 8, 8, (x, y) => Mathf.Abs(x * 2 - 7) + Mathf.Abs(y * 2 - 7) <= 8 ? Color.white : Color.clear);
        var seal = PixelSprite("GateSeal", 32, 32, (x, y) =>
        {
            int d = Mathf.Abs(x * 2 - 31) + Mathf.Abs(y * 2 - 31);
            return (d >= 28 && d <= 32) || (Mathf.Abs(x * 2 - 31) < 3 && Mathf.Abs(y * 2 - 31) < 18) || (Mathf.Abs(y * 2 - 31) < 3 && Mathf.Abs(x * 2 - 31) < 18) ? Color.white : Color.clear;
        });
        var settings = AssetDatabase.LoadAssetAtPath<ArenaSettings>("Assets/Settings/DefaultArena.asset");
        settings.swordWavePrefab = Wave("SwordWave", beam, spark, new Color(.5f, .9f, 1), new Vector2(.6f, 1.5f), .3f, 12, .8f);
        settings.twinSwordPrefab = Wave("GeneratedWave", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Excalibur.png"), spark, new Color(.65f, 1, .9f), new Vector2(.22f, .75f), .16f, 15, 3, true);
        settings.reflectedWavePrefab = Wave("ReflectedWave", beam, spark, new Color(.75f, .6f, 1), new Vector2(.5f, 1.2f), .25f, 14, 1);
        settings.holySwordPrefab = Wave("HolySword", beam, spark, new Color(1, .9f, .5f), new Vector2(2.8f, 6), 1.4f, 18, .65f);
        const string burstPath = "Assets/Prefabs/Weapons/PixelImpact.prefab";
        var burstObject = new GameObject("PixelImpact"); var burst = burstObject.AddComponent<PixelBurst>(); burst.sparks = new SpriteRenderer[8];
        for (int i = 0; i < 8; i++) burst.sparks[i] = Sprite("Spark " + i, burstObject.transform, spark, Vector2.zero, new Vector2(.12f, .12f), Mint, 10);
        PrefabUtility.SaveAsPrefabAsset(burstObject, burstPath); Object.DestroyImmediate(burstObject);
        settings.impactPrefab = AssetDatabase.LoadAssetAtPath<PixelBurst>(burstPath); EditorUtility.SetDirty(settings);
        const string gatePath = "Assets/Prefabs/Weapons/GateProjectile.prefab";
        var gateObject = PrefabUtility.LoadPrefabContents(gatePath); var gate = gateObject.GetComponent<GateProjectile>();
        gate.visual = Trails(gateObject.transform, spark, Gold, 1);
        gate.visual.seal = Sprite("Gate Seal", gate.visual.transform, seal, Vector2.zero, Vector2.one * 1.5f, Gold, 6).transform;
        PrefabUtility.SaveAsPrefabAsset(gateObject, gatePath); PrefabUtility.UnloadPrefabContents(gateObject);
    }
}
