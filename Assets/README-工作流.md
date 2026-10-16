# 从这里开始：传统 Unity 编辑工作流

**请打开 `Assets/Scenes/BladeArena.unity`。** 这是保存了实际对象与 Prefab 连接的游戏场景。

无需执行生成脚本，也无需按 Play 才能看到内容。若刚从外部更新文件，先用 Unity 的 Assets > Refresh 等待编译，再双击 BladeArena。

## 目录与修改位置

| 想改什么 | 修改位置 |
| --- | --- |
| 角色、武器、地面原图 | `Assets/Art`，PNG 的 `.meta` GUID 在迁移时保留 |
| 主角 | `Assets/Prefabs/Characters/Artoria.prefab` |
| 普通 / 精英敌人 | `Schoolgirl.prefab` / `SchoolgirlElite.prefab` |
| Boss 外观与基础属性 | `Gilgamesh.prefab` |
| 剑外观 / 碰撞半径 | `Assets/Prefabs/Weapons/OrbitBlade.prefab` |
| Boss 飞剑、预警时间、速度 | `Assets/Prefabs/Weapons/GateProjectile.prefab` |
| 小球外观 / 增加剑数 | `Assets/Prefabs/Pickups/BladeOrb.prefab` 的 Value |
| 地面 | `Assets/Prefabs/Environment/Courtyard.prefab` |
| UI 布局、文字字体、颜色 | `Assets/Prefabs/UI/GameHUD.prefab`，标准 Canvas / Text / Image / Button |
| 刷怪节奏、数量上限、拾取、Boss 时间与奖励 | `Assets/Settings/DefaultArena.asset` |
| 强化名字、稀有度、等级、数值与前置 | `Assets/Settings/Upgrades` 中对应资源 |
| 玩家远程攻击的图片和基本属性 | `Assets/Prefabs/Weapons` 中 SwordWave / GeneratedWave / ReflectedWave / ExcaliburCharge / PixelImpact |
| 玩法脚本 | `Assets/Gameplay` |

## 场景里的实例可以直接编辑

- `Actors/Artoria` 是玩家；另外两位 Schoolgirl 是开场敌人。可用移动工具调整位置。
- `Pickups` 下 24 个小球是开场摆放实例，可拖动、复制、删除，Value 可以独立修改。
- `Environment` 下是地面 Prefab 和可见边界。
- `Spawn Points` 下 8 个 Transform 控制后续刷怪位置。距离玩家过近的点会跳过。
- `Game HUD` 是 Canvas Prefab 实例。暂停、升级、结算、属性面板默认隐藏；停止播放时可临时勾选 Active 预览并编辑，保存前恢复隐藏。
- `Game Manager` 的 Inspector 明确引用规则配置、玩家、摄像机、UI、出生点和所有生成用 Prefab。替换某类角色可直接拖一个新 Prefab 到对应槽。

修改场景实例只影响当前场景。要让之后生成的所有敌人也改变，请编辑 Prefab，或通过 Overrides > Apply 将实例改动应用回对应 Prefab。不要把 Play 模式里的临时状态当成持久编辑。

## 角色的编辑方式

1. 双击角色 Prefab。
2. 展开 `Artwork — replace this Sprite`，在 SpriteRenderer 上换图、调 Color 或 Transform 大小和位置。
3. 选中角色根物体，在 Combatant 上调整 Maximum Health、攻击力、Move Speed、Orbit Radius、Rotation Speed。
4. 初始剑已保存在 `Weapons` 子物体下。改 Initial Blade Count 或 Orbit Radius 后，点击 Inspector 下的「按 Initial Blade Count 更新初始剑阵」更新编辑预览（支持 Undo）。
5. 运行时剑阵自动均匀排列；获得双层 / 三重剑阵后自动分圈。编辑器中的 Angle Offset 用于初始预览，直接改刀 Transform 不会改变运行时运动规则。

战斗使用组件中的圆形判定半径，而非 Physics2D Collider；选中角色/剑时可看到 Gizmos。身体接触不会造成伤害，武器和技能命中时才结算伤害。

战斗规则、27 项强化、伤害公式与数值选择见同目录的 `战斗与强化说明.md`。强化尺寸同时影响视觉和圆形判定。玩家基础生命 100、攻击力 10，初始剑阵容量 30，可无限强化增加，除蓄魔只可获得一次，其他强化没有等级上限。

## UI 与字体

界面使用标准 uGUI，可以拖动 RectTransform、换字体、修改字号。按 Tab 或属性按钮打开属性面板，查看时战斗暂停。面板中 27 个 OwnedUpgradeSlot 已保存，悬停显示当前效果。统计值和三选一内容由 GameHud 更新，标题和静态提示可直接编辑 Text。Button 的功能在 Bind 时由代码绑定；不要再次添加相同 OnClick 以免执行两次。

中文字体文件在 `Assets/Art/Fonts`，同时保留来源说明和 OFL 授权。无需依赖电脑安装微软雅黑。

## 保存、运行与扩展

- Ctrl+S 保存场景；Prefab 模式保存 Prefab。
- 重开通过重新加载已保存的场景恢复，因此你保存的位置、图片、属性和 UI 改动都会保留。
- 若新建自己的场景，请加入 Build Settings，才能使用重开。
- 随机刷怪/掉落仍在运行时实例化 Inspector 指定的 Prefab；这是普通 Unity 的动态生成方式。
- 调整竞技场半径时，同时调整 Environment 下的可见边界和地面覆盖范围。边界本身是可编辑对象，不会在运行时重建。
- `Tools > Blade Arena > 打开可编辑场景` 提供快速入口。「创建缺少的初始资源」仅用于首次搭建；已有场景时不会重置你的编辑。

旧版原型已清理；当前游戏使用 Assets/Gameplay 下的组件。

像素特效图位于 `Assets/Art/Effects`，为本项目绘制的程序化硬边像素图，Point 过滤、无压缩。每个远程 Prefab 的 Pixel Effects 子物体可直接修改拖尾位置、尺寸和颜色。`DefaultArena` 的特效引用可单独替换。显式菜单“应用像素界面与特效模板”会覆盖模板布局，日常编辑无需执行。


## 魔力、主动技能与声音

- `DefaultArena` 的「魔力与主动技能」可调整槽数、每秒 / 每击杀回复量、Q 的半径和转速倍率、R 的蓄力时长及推开半径。默认三槽，回复 0.01 格/秒、0.05 格/击杀。
- Q 时长和 R 倍率在 `Upgrades/RoundTable.asset` / `Excalibur.asset` 的 Amount（首级）及 Per Extra Level（以后每级）编辑。蓄魔 Unique 为真，其他特殊强化可重复升级。
- `GeneratedWave.prefab` 和 `ReflectedWave.prefab` 是独立可编辑的黄色单次剑气；`SwordWave.prefab` 是可暴击的贯穿剑气；`ExcaliburCharge.prefab` 是蓄力法阵和光束。旧 `HolySword.prefab` 仅保留为可复用素材，当前 R 使用蓄力法阵和全屏闪光。
- 音效文件在 `Assets/Audio`。双击 `Assets/Prefabs/Audio/GameAudio.prefab` 可替换 Clips、调整 Volume。Clips 顺序对应 GameAudio.cs 中的 SoundCue 枚举，保留 14 项顺序。
- `Tools/GeneratePixelAudio.py` 可重新生成默认音色，会覆盖同名 WAV；自行制作的音效请另命名或先备份。
- 敌人的 Combatant 中 Hit Tint 和 Hit Flash Duration 控制受击颜色及持续时间；默认红色、0.12 秒。
- GameHUD 的 Active Skills 是保存的魔力条和 E / Q / R 按钮。未获得蓄魔时隐藏；Holy Flash 是非交互的圣剑释放闪光。
- `ActiveRulesBuilder.Apply` 是本次资源迁移工具，会重建技能栏、声音和蓄力模板；正常修改 Prefab 后无需再次运行。
