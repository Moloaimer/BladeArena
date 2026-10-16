using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BladeArena;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class CombatUpgradeChecks
{
    static BladeGameController template;
    static int passed, frames, stage;
    static CombatUpgradeChecks() { EditorApplication.playModeStateChanged += OnPlay; }
    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception("UPGRADE_CHECK FAIL: " + name);
        passed++; Debug.Log("UPGRADE_CHECK PASS: " + name);
    }
    static bool Near(float a, float b) => Mathf.Abs(a-b) < .001f;
    static void Call(object o, string method, params object[] args) => o.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o,args);

    public static void AuthorAndRun() { ActiveRulesBuilder.Apply(); Run(); }
    public static void Run()
    {
        try {
            EditorSceneManager.OpenScene(EditableArenaBuilder.ScenePath);
            template=Object.FindObjectOfType<BladeGameController>();
            Check(template && PrefabUtility.IsPartOfPrefabInstance(template.player), "Authored player and scene preserved");
            var d=template.settings.upgrades;
            Check(d.Length==27 && d.All(x=>x) && d.Select(x=>x.id).Distinct().Count()==27,"27 unique serialized upgrades");
            Check(d.Count(x=>x.rarity==UpgradeRarity.Common)==14 && d.Count(x=>x.rarity==UpgradeRarity.Rare)==9 && d.Count(x=>x.rarity==UpgradeRarity.Special)==4,"14 common, 9 rare, 4 special");
            Check(d.Single(x=>x.id==UpgradeId.Excalibur).prerequisites.SequenceEqual(new[]{UpgradeId.ManaReservoir}),"Special requires mana only");
            Check(template.settings.commonWeight==75 && template.settings.rareWeight==20 && template.settings.specialWeight==5,"Weights 75/20/5 serialized");
            Check(template.settings.soundPrefab.clips.Length==14 && template.settings.soundPrefab.clips.All(c=>c && c.samples>0) && template.settings.soundPrefab.voices.Length==8,"14 imported audio clips and 8 voices");
            Check(template.settings.spellAuraPrefab && template.settings.spellAuraPrefab.rays.Length==5,"Saved charge effect prefab");
            Check(Object.FindObjectsOfType<Transform>(true).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No missing scripts in authored scene");
            SessionState.SetBool("CombatUpgradeChecks.Run",true);EditorApplication.isPlaying=true;
        } catch(Exception e){Fail(e);}
    }
    static void OnPlay(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CombatUpgradeChecks.Run",false))
        {SessionState.SetBool("CombatUpgradeChecks.Run",false);frames=stage=0;EditorApplication.update+=Tick;}
    }
    sealed class Fixture : IDisposable
    {
        public readonly BladeGameController g;
        readonly GameObject root;
        public Fixture()
        {
            root=new GameObject("Test fixture");
            g=root.AddComponent<BladeGameController>(); g.enabled=false;
            g.settings=Object.Instantiate(template.settings);
            g.settings.upgrades=template.settings.upgrades.Select(d=>Object.Instantiate(d)).ToArray();
            g.actorsRoot=new GameObject("Actors").transform;g.actorsRoot.SetParent(root.transform);
            g.pickupsRoot=new GameObject("Pickups").transform;g.pickupsRoot.SetParent(root.transform);
            g.projectilesRoot=new GameObject("Projectiles").transform;g.projectilesRoot.SetParent(root.transform);
            g.player=Object.Instantiate(AssetDatabase.LoadAssetAtPath<Combatant>("Assets/Prefabs/Characters/Artoria.prefab"),g.actorsRoot);
            g.enemyPrefab=template.enemyPrefab;g.elitePrefab=template.elitePrefab;g.bossPrefab=template.bossPrefab;
            g.orbPrefab=template.orbPrefab;g.projectilePrefab=template.projectilePrefab;
            g.gameCamera=template.gameCamera;
            g.hud=Object.Instantiate(template.hud,root.transform);
            Call(g,"Start");
        }
        public Combatant Enemy() => g.SpawnActor(g.enemyPrefab,new Vector2(3,0));
        public void Level(UpgradeId id,int count=1) { for(int i=0;i<count;i++) if(!g.AcquireUpgrade(id)) throw new Exception("Cannot acquire "+id); }
        public void Dispose()
        {
            foreach(var d in g.settings.upgrades) Object.DestroyImmediate(d);
            Object.DestroyImmediate(g.settings);Object.DestroyImmediate(root);
        }
    }
    static void MoveBladesAway(Combatant actor)
    { foreach(var b in actor.Blades) b.transform.position=new Vector2(-15,-15); }
    static void Clash(Fixture f,Combatant e)
    {
        if(f.g.player.Blades.Count==0)f.g.player.AddBlades(1,f.g.MaxBladeCount);
        if(e.Blades.Count==0)e.AddBlades(1,48);
        MoveBladesAway(f.g.player);MoveBladesAway(e);
        f.g.player.Blades[0].transform.position=new Vector2(1.5f,0);
        e.Blades[0].transform.position=new Vector2(1.5f,0);
        // Other blades are kept apart, outside either body.
        for(int i=1;i<e.Blades.Count;i++)e.Blades[i].transform.position=new Vector2(15,15);
        f.g.ResolveCombat(e);
    }

    static void Tick()
    {
        if(++frames<10)return;
        try {
            if(stage==1){
                var reset=Object.FindObjectsOfType<BladeGameController>().FirstOrDefault(g=>g.gameObject.name=="Game Manager");
                if(!reset || reset==template)return;reset.enabled=false;
                Check(reset.Level==1 && reset.player.health==100 && reset.player.Blades.Count==3 && reset.MaxBladeCount==30 && reset.ManaSlots==0 && reset.Mana==0,"Restart resets health, swords, upgrades and mana");
                Debug.Log("UPGRADE_CHECK ALL PASSED: "+passed);EditorApplication.update-=Tick;EditorApplication.Exit(0);return;
            }
            AudioListener.pause=true;
            template=Object.FindObjectOfType<BladeGameController>();template.enabled=false;
            TestStats();TestContacts();TestDamage();TestFormations();TestWaves();TestMana();TestSkills();TestDefense();TestUI();TestBoss();
            stage=1;template.RestartRun();
        } catch(Exception e){Fail(e);}
    }
    static void TestStats()
    {
        using(var f=new Fixture()){
            var g=f.g;float speed=g.player.rotationSpeed,radius=g.player.orbitRadius,move=g.player.moveSpeed;
            Check(g.player.health==100 && g.AttackPower==10 && g.MaxBladeCount==30 && g.player.Blades.Count==3,"Initial 100 HP, 10 attack, 3/30 swords");
            f.Level(UpgradeId.Capacity,100);Check(g.MaxBladeCount==530 && g.player.Blades.Count==3,"Capacity grows without granting swords");
            f.Level(UpgradeId.Damage,100);Check(Near(g.AttackPower,30),"Attack uses base times one plus 20 percent sqrt rank");
            f.Level(UpgradeId.OrbitSpeed,100);f.Level(UpgradeId.Radius,100);f.Level(UpgradeId.MoveSpeed,100);f.Level(UpgradeId.Width,100);
            Check(Near(g.player.rotationSpeed,speed*2.2f)&&Near(g.player.orbitRadius,radius*2)&&Near(g.player.moveSpeed,move*2),"Speed, radius and movement use diminishing growth");
            Check(Near(g.player.Blades[0].CollisionRadius,g.player.bladePrefab.hitRadius*2),"Visual enlargement scales collision radius");
            f.Level(UpgradeId.Health);Check(g.player.maximumHealth==110 && g.player.health==110,"Health upgrade heals 20 percent of new maximum with clamp");
            Check(g.ResolveCritical(10,0)==10,"Zero critical chance cannot crit even roll zero");
            f.Level(UpgradeId.Critical,10);Check(g.ResolveCritical(10,1)==20,"100 percent crit is guaranteed");
            f.Level(UpgradeId.Critical,3);Check(Near(g.ResolveCritical(10,.99f),23),"130 percent converts to guaranteed 230 percent damage");
            g.RecalculateStats();g.RecalculateStats();Check(Near(g.CriticalMultiplier,2.3f),"Recalculation never reapplies critical overflow");
            f.Level(UpgradeId.Critical,7);Check(Near(g.CriticalMultiplier,3),"200 percent rate yields 300 percent damage");
        }
    }
    static void TestContacts()
    {
        using(var f=new Fixture()){
            var g=f.g;var e=f.Enemy();e.health=1000;MoveBladesAway(g.player);MoveBladesAway(e);
            var b=g.player.Blades[0];b.transform.position=e.Position;g.HitBody(g.player,e);g.HitBody(g.player,e);
            Check(e.health==990 && g.player.Blades.Count==3,"Sword body contact once, player sword retained");
            var pos=e.Position;Check(e.HitFlashRemaining>0 && e.body.color==e.hitTint && e.invulnerability==0,"Enemy flashes red without hit invulnerability");
            g.player.Blades[1].transform.position=e.Position;g.HitBody(g.player,e);Check(e.health==980 && e.Position==pos,"Different sword deals damage without pushing enemy");
            MoveBladesAway(g.player);g.HitBody(g.player,e);b.transform.position=e.Position;g.HitBody(g.player,e);Check(e.health==970,"Separation and reentry allow next hit");
            e.Step(Vector2.zero,.2f,0,35);Check(e.HitFlashRemaining==0 && e.body.color!=e.hitTint,"Red tint restores after feedback duration");
            MoveBladesAway(g.player);MoveBladesAway(e);int count=e.Blades.Count;
            e.Blades[0].transform.position=g.player.Position;g.HitBody(e,g.player);g.HitBody(e,g.player);
            Check(g.player.health==90 && e.Blades.Count==count,"Normal enemy hit retains sword through same overlap");
            e.Blades[0].transform.position=new Vector2(15,15);g.HitBody(e,g.player);e.Blades[0].transform.position=g.player.Position;g.HitBody(e,g.player);
            Check(g.player.health==90 && e.Blades.Count==count-1,"Reentry during invulnerability consumes enemy sword");
        }
        using(var f=new Fixture()){
            var e=f.Enemy();int p=f.g.player.Blades.Count,n=e.Blades.Count;Clash(f,e);
            Check(f.g.player.Blades.Count==p-1 && e.Blades.Count==n-1,"Clash destroys both weapons immediately");
            f.g.ResolveCombat(e);Check(f.g.player.Blades.Count==p-1 && e.Blades.Count==n-1,"Same clash cannot resolve twice");
        }
        using(var f=new Fixture()){
            var g=f.g;MoveBladesAway(g.player);
            var shot=Object.Instantiate(g.projectilePrefab,g.player.Position,Quaternion.identity,g.projectilesRoot);shot.age=shot.warningDuration;shot.direction=Vector2.zero;g.Projectiles.Add(shot);
            Call(g,"UpdateProjectiles",0f);Call(g,"UpdateProjectiles",0f);
            Check(g.player.health==90 && g.Projectiles.Count==1,"Gate hit retains projectile without duplicate overlap damage");
            shot.transform.position=Vector2.one*10;Call(g,"UpdateProjectiles",0f);shot.transform.position=g.player.Position;Call(g,"UpdateProjectiles",0f);
            Check(g.Projectiles.Count==0 && g.player.health==90,"Blocked Gate reentry consumes projectile");
        }
    }
    static void TestDamage()
    {
        using(var f=new Fixture()){
            var g=f.g;var e=f.Enemy();g.player.AddBlades(27,30);f.Level(UpgradeId.Damage);f.Level(UpgradeId.Rally);f.Level(UpgradeId.Combo);f.Level(UpgradeId.ArmorPiercing);
            Check(Near(g.ResolvePlayerDamage(e,1,true,1),13.8f) && Near(g.ResolvePlayerDamage(e,2,false,1),24),"Rally affects direct damage, skills use attack and own multiplier");
            e.role=CharacterRole.Elite;
            Check(Near(g.ResolvePlayerDamage(e,1,true,1),15.6f) && Near(g.ResolvePlayerDamage(e,2,false,1),27.6f),"Elite bonus applies to both types in additive bonus pool");
            g.CalculateBladeDamage(g.player.Blades[0],e);g.CalculateBladeDamage(g.player.Blades[1],e);
            Check(Near(g.ComboDamageBonus(e),.1f) && Near(g.ResolvePlayerDamage(e,1,true,1),16.8f),"Different swords build one combo stack after first hit");
            g.CalculateBladeDamage(g.player.Blades[1],e);Check(Near(g.ComboDamageBonus(e),.1f),"Same sword preserves but does not add combo stacks");
            f.Level(UpgradeId.Critical,13);
            Check(Near(g.ResolvePlayerDamage(e,1,false,0,false),13.8f),"Yellow skill ignores crit but receives elite bonus");
            Check(Near(g.ResolvePlayerDamage(e,2,false,0),63.48f),"Other skill damage shares critical formula");
            Set(g,"Elapsed",2f);Check(g.ComboDamageBonus(e)==0,"Combo expires after 1.5 seconds without hit");
        }
    }
    static void Set(object o,string property,object value)=>o.GetType().GetProperty(property).SetValue(o,value,null);
    static void TestFormations()
    {
        using(var f=new Fixture()){
            var g=f.g;Check(!g.AcquireUpgrade(UpgradeId.TripleRing),"Triple ring requires double ring and 30 current swords");
            f.Level(UpgradeId.Capacity,3);g.player.AddBlades(42,45);Check(g.player.RingCount==1,"Capacity alone never changes layout");
            f.Level(UpgradeId.DoubleRing);f.Level(UpgradeId.TripleRing);
            Check(g.player.Blades.Count(b=>b.Ring==0)==10 && g.player.Blades.Count(b=>b.Ring==1)==10 && g.player.Blades.Count(b=>b.Ring==2)==25,"First 30 evenly fill three rings; extras fill outer ring");
            while(g.player.Blades.Count>12)g.player.BreakBlade(g.player.Blades.Count-1);
            g.player.Step(Vector2.zero,0,0,35);f.Level(UpgradeId.TripleRing);
            Check(g.player.RingCount==3 && g.player.Blades.Count(b=>b.Ring==2)==4,"Unlocked triple persists and repeats below 30 swords");
            Check(g.player.Blades.All(b=>!float.IsNaN(b.transform.position.x)),"Rearrangement produces finite positions after shatters");
        }
    }
    static void TestWaves()
    {
        using(var f=new Fixture()){
            var g=f.g;var near=f.Enemy();near.health=1000;var far=f.Enemy();far.transform.position=new Vector2(8,0);far.health=1000;
            f.Level(UpgradeId.WaveGeneration,13);f.Level(UpgradeId.Critical,13);f.Level(UpgradeId.PiercingWave);g.player.AddBlades(27,30);MoveBladesAway(g.player);
            g.player.Blades[0].transform.position=near.Position;g.HitBody(g.player,near);g.HitBody(g.player,near);
            Check(g.Waves.Count==2 && g.Waves.All(w=>w.homing&&w.singleHit&&!w.canCrit&&w.target==near) && g.player.Blades.Count==30,"200 percent generation creates two independent single-hit spells at full capacity");
            int before=near.health;for(int i=0;i<40;i++)Call(g,"UpdateBuildEffects",.02f);
            Check(near.health==before-20 && far.health==1000 && g.Waves.Count==0,"Yellow waves hit once each without crit, piercing or recursive procs");
            g.CollectOrb(3);Check(g.Waves.Count==0 && g.player.Blades.Count==30,"Pickups do not trigger wave generation or exceed capacity");
            Call(g,"OnBladeHit",g.player.Blades[0]);var wave=g.Waves[0];near.alive=false;Call(g,"UpdateBuildEffects",.01f);Check(wave.target==far,"Generated wave retargets living enemy");
            far.alive=false;Call(g,"UpdateBuildEffects",4f);Check(g.Waves.Count==0,"Targetless waves expire");
        }
        using(var f=new Fixture()){
            var g=f.g;var e=f.Enemy();f.Level(UpgradeId.Reflect,19);Clash(f,e);
            Check(g.Waves.Count==1 && !g.Waves[0].canCrit && !g.Has(UpgradeId.WaveGeneration),"Reflection rolls independently at 100 percent without common upgrade");
        }
    }
    static void TestMana()
    {
        using(var f=new Fixture()){
            var g=f.g;g.AddMana(3);Check(g.Mana==0 && !g.AcquireUpgrade(UpgradeId.RoundTable),"Mana and special skills locked before reservoir");
            f.Level(UpgradeId.ManaReservoir);Check(g.ManaSlots==3 && g.Mana==0 && !g.AcquireUpgrade(UpgradeId.ManaReservoir),"Reservoir unlocks three empty slots once only");
            Call(g,"UpdateActiveSkills",100f);Check(Near(g.Mana,1),"100 active seconds restore one mana");
            var e=f.Enemy();g.Damage(e,10000);g.Damage(e,10000);Check(Near(g.Mana,1.05f),"One kill restores 0.05 mana once");
            g.AddMana(100);Check(g.Mana==3,"Mana stops at three slots");
            Check(g.TryRefillSwords() && g.Mana==2 && g.player.Blades.Count==30,"E spends one mana and fills capacity");
            Check(!g.TryRefillSwords() && g.Mana==2,"E at full capacity costs nothing");
            g.TogglePause();g.Simulate(5);Check(g.Mana==2 && !g.TryRefillSwords(),"Pause freezes mana and rejects casts");
            g.TogglePause();g.ToggleAttributes();g.Simulate(5);Check(g.Mana==2,"Tab panel freezes mana");
        }
    }
    static void TestSkills()
    {
        using(var f=new Fixture()){
            var g=f.g;var e=f.Enemy();f.Level(UpgradeId.ManaReservoir);f.Level(UpgradeId.RoundTable);f.Level(UpgradeId.Recycle);f.Level(UpgradeId.ShatterBurst);f.Level(UpgradeId.MagicReforge);f.Level(UpgradeId.Parry);
            float r=g.player.orbitRadius,s=g.player.rotationSpeed;g.AddMana(3);Check(g.TryRoundTable() && g.Mana==1 && g.RoundTableRemaining==20,"Q costs two mana and lasts 20 seconds");
            Check(Near(g.player.orbitRadius,r*1.5f) && Near(g.player.rotationSpeed,s*1.6f) && g.player.ChaoticFormation,"Q expands range and speeds irregular orbit");
            int n=g.player.Blades.Count;Clash(f,e);Call(g,"UpdateBuildEffects",0f);
            Check(g.player.Blades.Count==n && g.AvalonEnergy==0 && e.health==e.maximumHealth && g.player.invulnerability>.19f,"Q blocks without breaking player swords or firing shatter effects, parry still triggers");
            e.AddBlades(3,48);MoveBladesAway(g.player);g.player.Blades[0].transform.position=new Vector2(1.5f,0);foreach(var other in e.Blades)other.transform.position=new Vector2(1.5f,0);g.ResolveCombat(e);
            Check(e.Blades.Count==0 && g.player.Blades.Count==n,"Indestructible Q sword intercepts all overlapping enemy blades in same pass");
            g.AddMana(3);Check(!g.TryRoundTable() && g.Mana==3,"Active Q cannot refresh or waste mana");
            f.Level(UpgradeId.RoundTable);Check(g.RoundTableDuration==22,"Repeat Q upgrade extends future duration");
            Call(g,"UpdateActiveSkills",20f);Check(!g.RoundTableActive && Near(g.player.orbitRadius,r) && Near(g.player.rotationSpeed,s),"Q expiry restores base orbit stats");
        }
        using(var f=new Fixture()){
            var g=f.g;f.Level(UpgradeId.ManaReservoir);f.Level(UpgradeId.Excalibur);g.AddMana(3);
            var e=f.Enemy();e.health=1000;e.moveSpeed=0;var far=f.Enemy();far.transform.position=new Vector2(30,30);far.health=1000;far.moveSpeed=0;
            int count=g.player.Blades.Count;Check(g.TryExcalibur() && g.Mana==0 && g.IsCharging && g.player.SwordsHidden && !g.player.weaponRoot.gameObject.activeSelf,"R spends three mana and hides swords while charging");
            Check(Vector2.Distance(e.Position,g.player.Position)>=g.settings.excaliburPushRadius,"R charge pushes nearby enemies away");
            var pos=g.player.Position;typeof(BladeGameController).GetField("input",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,Vector2.right);g.Simulate(.1f);
            Check(g.player.Position==pos && g.IsCharging,"R charge prevents movement");
            g.Damage(g.player,10);Check(g.player.health==90,"R super armor does not grant damage immunity");
            g.TogglePause();float time=g.ChargeRemaining;g.Simulate(2);Check(g.ChargeRemaining==time,"Pause freezes charge");g.TogglePause();
            Call(g,"UpdateActiveSkills",1f);
            Check(!g.IsCharging && !g.player.SwordsHidden && g.player.Blades.Count==count,"R restores same swords after charge");
            Check(e.health==900 && far.health==1000,"R hits visible enemy once at 1000 percent, leaves offscreen enemy untouched");
            f.Level(UpgradeId.Excalibur);Check(g.ExcaliburMultiplier==12,"Repeat R upgrade adds 200 percentage points");
            g.AddMana(3);g.TryExcalibur();g.player.invulnerability=0;g.Damage(g.player,10000);
            Check(!g.IsCharging && !g.player.SwordsHidden,"Death cancels unfinished charge and restores weapon state");
        }
    }
    static void TestDefense()
    {
        using(var f=new Fixture()){
            var g=f.g;var e=f.Enemy();f.Level(UpgradeId.Recycle,25);Clash(f,e);
            Check(g.Shields==2 && Near(g.AvalonEnergy,50),"Large recycle payout converts multiple shields and keeps remainder");
            int hp=g.player.health;Check(!g.Damage(g.player,999) && g.Shields==1 && g.player.health==hp,"Shield absorbs before lethal damage");
        }
        using(var f=new Fixture()){
            var g=f.g;var e=f.Enemy();f.Level(UpgradeId.LakeBlessing);for(int i=0;i<9;i++)Clash(f,e);f.Level(UpgradeId.LakeBlessing);Clash(f,e);
            Check(g.Shields==1 && g.ShieldCapacity==5,"Lake repeats preserve counter and fixed ten-block threshold");
        }
        using(var f=new Fixture()){
            var g=f.g;var e=f.Enemy();g.player.AddBlades(27,30);f.Level(UpgradeId.MagicReforge);
            for(int i=0;i<5;i++)Clash(f,e);Call(g,"UpdateBuildEffects",0f);
            Check(g.player.Blades.Count==26,"Five shatters at first magic reforge rank return one sword");
            f.Level(UpgradeId.Reforge,100);f.Level(UpgradeId.MagicReforge,100);
            Check(g.QuickReforgeChance<1 && g.MagicReforgeCredit<1 && g.QuickReforgeChance>.94f,"Refund probability has diminishing uncapped ranks below 100 percent");
        }
        using(var f=new Fixture()){
            var g=f.g;f.Level(UpgradeId.ManaReservoir);f.Level(UpgradeId.Avalon);g.player.AddBlades(27,30);g.player.health=1;g.Damage(g.player,100);
            Check(g.player.alive && g.player.health==1 && g.player.Blades.Count==24 && Near(g.AvalonReadyIn,60)&&Near(g.player.invulnerability,2),"Avalon consumes ceil 20 percent, leaves one HP and grants two seconds immunity");
            Check(g.AvalonEnergy==0,"Avalon sacrifice does not generate recycle energy");
            f.Level(UpgradeId.Avalon,100);Check(g.AvalonCooldown>3 && g.AvalonCooldown<7 && Near(g.AvalonReadyIn,60),"Avalon upgrade affects next cooldown, cannot reset active cooldown");
            g.player.invulnerability=0;g.Damage(g.player,100);Check(!g.player.alive,"Avalon cannot trigger again during cooldown");
        }
    }
    static void TestUI()
    {
        using(var f=new Fixture()){
            var g=f.g;f.Level(UpgradeId.ManaReservoir);f.Level(UpgradeId.Critical,13);f.Level(UpgradeId.WaveGeneration,10);
            bool poolValid=true;for(int i=0;i<100;i++){g.OfferUpgrade();poolValid &= g.Choices.Distinct().Count()==3 && g.Choices.All(c=>g.CanAcquire(g.Definition((UpgradeId)c)) && (UpgradeId)c!=UpgradeId.ManaReservoir);}Check(poolValid,"100 draws contain distinct eligible upgrades and exclude unique reservoir");
            g.ChooseUpgrade(0);g.hud.attributeButton.onClick.Invoke();g.hud.Refresh();float elapsed=g.Elapsed;g.Simulate(1);
            Check(g.State==RunState.Attributes&&g.Elapsed==elapsed&&g.hud.attributeStats.text.Contains("230%"),"Tab shows actual stats and freezes play");
            var slot=g.hud.ownedSlots.First(s=>s.gameObject.activeSelf&&s.label.text.Contains("剑气生成"));slot.OnPointerEnter(null);
            Check(g.hud.tooltipBody.text.Contains("55%")&&g.hud.tooltipTitle.text.Contains("Lv.10"),"Hover displays rank and actual overflow rate");
            g.hud.closeAttributes.onClick.Invoke();g.TogglePause();g.ToggleAttributes();g.TogglePause();Check(g.State==RunState.Paused,"Attributes preserve previous pause state");g.TogglePause();
            g.player.AddBlades(100,g.MaxBladeCount);
            foreach(var id in new[]{UpgradeId.ManaReservoir,UpgradeId.DoubleRing,UpgradeId.TripleRing})if(!g.Has(id))f.Level(id);
            foreach(UpgradeId id in Enum.GetValues(typeof(UpgradeId)))if(!g.Has(id))f.Level(id);
            g.ToggleAttributes();g.hud.Refresh();g.hud.RefreshAttributes();
            bool textFits=true;
            foreach(var def in g.settings.upgrades){g.hud.ShowUpgrade(def);Canvas.ForceUpdateCanvases();textFits &= g.hud.tooltipBody.preferredHeight <= g.hud.tooltipBody.rectTransform.rect.height + 1;}
            Check(textFits,"All 27 hover descriptions fit saved panel");
            Check(g.hud.ownedSlots.Length==27 && g.hud.ownedSlots.All(s=>s.gameObject.activeSelf),"All 27 upgrades fit saved UI slots");
            Check(g.hud.manaFills.Length==3 && g.hud.refillButton && g.hud.roundTableButton && g.hud.excaliburButton,"Saved mana and skill controls resolve");
            template.hud.gameObject.SetActive(false);template.player.gameObject.SetActive(false);g.hud.ShowUpgrade(g.Definition(UpgradeId.Excalibur));Capture(g,"attributes-preview.png");
            g.ToggleAttributes();g.AddMana(3);g.TryRoundTable();g.hud.Refresh();Capture(g,"round-table-preview.png");
            g.AddMana(3);g.TryExcalibur();Call(g,"UpdateActiveSkills",.5f);g.hud.Refresh();Capture(g,"charge-preview.png");
            g.OfferUpgrade();g.Choices[0]=(int)UpgradeId.WaveGeneration;g.Choices[1]=(int)UpgradeId.RoundTable;g.Choices[2]=(int)UpgradeId.Excalibur;g.hud.Refresh();Canvas.ForceUpdateCanvases();
            Check(g.hud.upgradeDescriptions.All(t=>t.preferredHeight<=t.rectTransform.rect.height+1),"Full upgrade descriptions and next-rank values fit cards");Capture(g,"upgrade-preview.png");
            template.hud.gameObject.SetActive(true);template.player.gameObject.SetActive(true);
        }
    }
    static void TestBoss()
    {
        using(var f=new Fixture()){
            var g=f.g;Set(g,"Elapsed",60f);g.SpawnBoss();Check(g.Boss && g.Boss.maximumHealth==1500,"First boss includes endless health growth");
            g.SpawnVolley(3);Check(g.Projectiles.Count==3 && g.Projectiles.All(s=>!s.Launched),"Gate volley starts with warning");
            g.Damage(g.Boss,100000);Call(g,"UpdateBuildEffects",0f);
            Check(!g.Boss && g.Projectiles.Count==0 && g.BossesDefeated==1 && g.player.health==100 && g.player.Blades.Count<=g.MaxBladeCount,"Boss death clears projectiles, heals and obeys sword capacity");
        }
    }
    static void Capture(BladeGameController g,string filename)
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        var canvas=g.hud.GetComponent<Canvas>();var camera=g.gameCamera;
        int sortingOrder=canvas.sortingOrder;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=100;
        var rt=new RenderTexture(1440,900,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1440,900,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath,"../"+filename),tex.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;canvas.sortingOrder=sortingOrder;
    }
    static void Fail(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
}
