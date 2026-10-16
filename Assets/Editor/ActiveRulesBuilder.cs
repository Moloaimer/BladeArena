using System;
using System.IO;
using System.Linq;
using BladeArena;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit migration: keep authored scene, prefab GUIDs and existing UI; add saved objects.
public static class ActiveRulesBuilder
{
    public static void Apply()
    {
        var settings = AssetDatabase.LoadAssetAtPath<ArenaSettings>("Assets/Settings/DefaultArena.asset");
        settings.commonWeight = 75; settings.rareWeight = 20; settings.specialWeight = 5;
        settings.upgrades = AssetDatabase.FindAssets("t:UpgradeDefinition", new[] { "Assets/Settings/Upgrades" })
            .Select(g => AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(d => (int)d.id).ToArray();
        MakeAudio(settings); MakeAura(settings); UpdateWave(settings.twinSwordPrefab); UpdateWave(settings.reflectedWavePrefab); UpdateHud();
        EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
    }
    static void MakeAudio(ArenaSettings settings)
    {
        Directory.CreateDirectory("Assets/Prefabs/Audio"); AssetDatabase.Refresh();
        const string path = "Assets/Prefabs/Audio/GameAudio.prefab";
        var root = new GameObject("Game Audio"); var audio = root.AddComponent<GameAudio>();
        audio.clips = Enum.GetNames(typeof(SoundCue)).Select(n => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + n + ".wav")).ToArray();
        if (audio.clips.Any(c => !c)) throw new Exception("Missing generated WAV file");
        audio.voices = new AudioSource[8];
        for (int i = 0; i < audio.voices.Length; i++)
        {
            var go = new GameObject("Voice " + (i + 1)); go.transform.SetParent(root.transform);
            var source = go.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0; source.volume = .55f;
            audio.voices[i] = source;
        }
        PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root);
        settings.soundPrefab = AssetDatabase.LoadAssetAtPath<GameAudio>(path);
    }
    static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = color; sr.sortingOrder = 9;
        go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1); return sr;
    }
    static void MakeAura(ArenaSettings settings)
    {
        var root = new GameObject("Excalibur Charge"); var aura = root.AddComponent<SpellAura>();
        var sealRoot = new GameObject("Rotating Seal"); sealRoot.transform.SetParent(root.transform, false); aura.seal = sealRoot.transform;
        var seal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/GateSeal.png");
        var beam = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/SwordBeam.png");
        var ring = Sprite("Golden Seal", sealRoot.transform, seal, Vector2.one * 2.8f, Vector2.zero, new Color(1, .8f, .3f, .5f));
        ring.sortingOrder = 3;
        aura.rays = new SpriteRenderer[5];
        for (int i = 0; i < 5; i++) aura.rays[i] = Sprite("Charge Ray " + i, root.transform, beam, new Vector2(.2f, 2.5f + i % 2), new Vector2((i - 2) * .3f, .5f), new Color(1, .9f, .4f, .5f));
        const string path = "Assets/Prefabs/Weapons/ExcaliburCharge.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root);
        settings.spellAuraPrefab = AssetDatabase.LoadAssetAtPath<SpellAura>(path);
    }
    static void UpdateWave(SwordWave prefab)
    {
        var path = AssetDatabase.GetAssetPath(prefab); var root = PrefabUtility.LoadPrefabContents(path); var wave = root.GetComponent<SwordWave>();
        wave.homing = true; wave.singleHit = true; wave.canCrit = false;
        wave.artwork.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/SwordBeam.png");
        wave.artwork.transform.localScale = new Vector3(.28f / wave.artwork.sprite.bounds.size.x, .8f / wave.artwork.sprite.bounds.size.y, 1);
        wave.artwork.color = new Color(1, .87f, .3f);
        foreach (var sr in wave.visual.trailPixels) sr.color = new Color(1, .78f, .25f, .6f);
        PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
    }
    static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = Vector2.one * .5f; r.sizeDelta = size; r.anchoredPosition = pos; return r;
    }
    static Text Text(string name, Transform parent, string value, Vector2 size, Vector2 pos, int fontSize)
    {
        var t = Rect(name, parent, size, pos).gameObject.AddComponent<Text>();
        t.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/NotoSansCJKsc-Regular.otf"); t.fontSize = fontSize;
        t.text = value; t.color = new Color(1, .86f, .45f); t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        t.verticalOverflow = VerticalWrapMode.Overflow; return t;
    }
    static Button Button(string name, Transform parent, string label, float x)
    {
        var image = Rect(name, parent, new Vector2(214, 38), new Vector2(x, -22)).gameObject.AddComponent<Image>(); image.color = new Color(.08f, .13f, .22f, .97f);
        var b = image.gameObject.AddComponent<Button>(); b.targetGraphic = image;
        Text("Label", b.transform, label, new Vector2(208, 36), Vector2.zero, 18);
        return b;
    }
    static void UpdateHud()
    {
        const string path = "Assets/Prefabs/UI/GameHUD.prefab"; var root = PrefabUtility.LoadPrefabContents(path); var hud = root.GetComponent<GameHud>();
        var slots = hud.ownedSlots.ToList();
        if (slots.Count < 27)
        {
            var clone = Object.Instantiate(slots[0], slots[0].transform.parent); clone.name = "Upgrade Slot 27"; slots.Add(clone);
        }
        hud.ownedSlots = slots.ToArray();
        for (int i = 0; i < slots.Count; i++)
        {
            var r = slots[i].GetComponent<RectTransform>(); r.anchoredPosition = new Vector2(i % 2 == 0 ? -158 : 158, 230 - i / 2 * 32);
            r.sizeDelta = new Vector2(304, 29); slots[i].label.fontSize = 16;
        }
        foreach (var t in root.GetComponentsInChildren<Text>(true))
            t.text = t.text.Replace("飞刀", "剑").Replace("刀阵", "剑阵").Replace("玩家刀", "玩家剑").Replace("刀碰刀", "剑碰敌刀").Replace("碎刀", "碎剑").Replace("26", "27");
        var old = root.transform.Find("Active Skills"); if (old) Object.DestroyImmediate(old.gameObject);
        var bar = Rect("Active Skills", root.transform, new Vector2(688, 94), new Vector2(0, 96)); bar.anchorMin = bar.anchorMax = new Vector2(.5f, 0);
        var bg = bar.gameObject.AddComponent<Image>(); bg.color = new Color(.025f, .04f, .085f, .95f); hud.skillBar = bar.gameObject;
        hud.manaLabel = Text("Mana", bar, "魔力 0 / 3", new Vector2(162, 30), new Vector2(-242, 22), 17);
        hud.activeSkillLabel = Text("Status", bar, "每 100 秒恢复 1 格", new Vector2(200, 30), new Vector2(226, 22), 15);
        hud.manaFills = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var back = Rect("Mana Slot " + i, bar, new Vector2(74, 20), new Vector2(-91 + i * 80, 22)).gameObject.AddComponent<Image>(); back.color = new Color(.22f, .25f, .32f);
            var fill = Rect("Fill", back.transform, new Vector2(68, 14), Vector2.zero).gameObject.AddComponent<Image>();
            fill.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Square.png"); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0;
            fill.color = new Color(.45f, .75f, 1); hud.manaFills[i] = fill;
        }
        hud.refillButton = Button("E Refill", bar, "E 补满剑阵 · 1 格", -226);
        hud.roundTableButton = Button("Q Round Table", bar, "Q 圆桌剑阵 · 2 格", 0);
        hud.excaliburButton = Button("R Excalibur", bar, "R 圣剑 · 3 格", 226);
        bar.gameObject.SetActive(false);
        if (!hud.holyFlash)
        {
            var r = Rect("Holy Flash", root.transform, Vector2.zero, Vector2.zero); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.SetAsFirstSibling();
            hud.holyFlash = r.gameObject.AddComponent<Image>(); hud.holyFlash.raycastTarget = false;
        }
        hud.holyFlash.color = new Color(1, .86f, .38f, 0);
        var rules = hud.pausePanel.transform.Find("Rules").GetComponent<Text>(); rules.text = "剑与敌刀相撞：双方破碎\n敌刀正常命中后保留，被抵挡时消失\nE 补剑 / Q 剑阵 / R 圣剑（需要解锁）";
        root.transform.Find("Controls").GetComponent<Text>().text = "WASD 移动  /  TAB 属性  /  E 补剑  Q 剑阵  R 圣剑  /  ESC 暂停";
        for (int i = 0; i < 3; i++)
        {
            var card = hud.upgradeButtons[i].GetComponent<RectTransform>(); card.sizeDelta = new Vector2(680, 164); card.anchoredPosition = new Vector2(0, 180 - i * 180);
            hud.upgradeTitles[i].rectTransform.sizeDelta = new Vector2(632, 36); hud.upgradeTitles[i].rectTransform.anchoredPosition = new Vector2(0, 57);
            hud.upgradeDescriptions[i].rectTransform.sizeDelta = new Vector2(632, 108); hud.upgradeDescriptions[i].rectTransform.anchoredPosition = new Vector2(0, -19); hud.upgradeDescriptions[i].fontSize = 17;
        }
        hud.upgradePanel.transform.Find("Heading").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 337);
        hud.upgradePanel.transform.Find("Hint").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 289);
        hud.pausePanel.transform.SetAsLastSibling(); hud.upgradePanel.transform.SetAsLastSibling(); hud.deathPanel.transform.SetAsLastSibling();
        hud.attributePanel.transform.SetAsLastSibling();
        PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
    }
}
