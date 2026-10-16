# 剑之誓约 · 可编辑 Unity 项目

**入口：Assets/Scenes/BladeArena.unity。** 停止播放也能看到角色、剑、地面和小球。资源刚更新时可执行 Assets > Refresh，编译完成后打开场景。

## 操作

WASD / 方向键移动，也支持鼠标或触屏拖动。Tab 查看属性和已有强化，悬停显示当前效果；Esc 暂停；升级按 1 / 2 / 3 或点击。

获得蓄魔后解锁三个魔力槽及 E 补满剑阵。获得对应特殊强化后，Q 消耗两格魔力开启圆桌剑阵，R 消耗三格魔力蓄力释放 Excalibur。死亡界面的 R 用于重开。

## 当前规则

初始生命 100、攻击力 10、持剑数 3、剑阵容量 30。27 项强化分为 14 普通、9 稀有、4 特殊，权重 75 / 20 / 5。蓄魔只获得一次，其余无等级上限；容量每级增加 5，但不赠送剑。

剑造成直接伤害，剑气等造成技能伤害；统一按“攻击力 × 技能倍率 ×（1 + 适用伤害加成）”计算。基础暴击率 0%、暴击伤害 200%，暴击率溢出 100% 的部分按 1:1 转为暴击伤害。剑气生成与反射产生的黄色单次剑气不暴击。

剑命中敌人身体后保留；剑碰敌刀双方破碎，Q 期间玩家剑不碎。敌刀正常命中玩家后保留，被无敌、护盾或 Avalon 抵挡时消失；同一次持续接触只结算一次。敌人受击变红，不产生受击无敌或普通受击击退。

完整效果、伤害示例、技能解锁和冲突处理见 **[战斗与强化说明](Assets/战斗与强化说明.md)**。

无尽敌人随时间增强，第 60 秒英雄王登场，王之财宝有 1.1 秒预警。击败后清除飞剑、恢复生命、补剑并获得经验，90 秒后出现下一轮 Boss。

## 编辑资源

| 内容 | 位置 |
| --- | --- |
| 主场景 | Assets/Scenes/BladeArena.unity |
| 角色、武器、地面、像素特效、中文字体 | Assets/Art |
| 玩家 / 普通敌人 / 精英 / Boss | Assets/Prefabs/Characters |
| 环绕剑、王之财宝、剑气、圣剑蓄力特效 | Assets/Prefabs/Weapons |
| 小球 / 地面 | Assets/Prefabs/Pickups / Environment |
| 像素 HUD、Tab 属性、技能按钮 | Assets/Prefabs/UI/GameHUD.prefab |
| 14 个 WAV 及音效通道 | Assets/Audio、Assets/Prefabs/Audio/GameAudio.prefab |
| 战斗、魔力、刷怪和技能配置 | Assets/Settings/DefaultArena.asset |
| 27 项强化 | Assets/Settings/Upgrades |
| 游戏逻辑 | Assets/Gameplay |

场景中保留真实的 Prefab 实例，包括玩家、两名开场敌人、24 个小球与可拖动出生点。运行时从 Inspector 指定的 Prefab 生成后续对象。修改实例仅影响该实例；希望之后生成的对象一起改变时，请编辑 Prefab。重开会重新加载保存的场景。

详细换图、改属性和管理方式见 **[传统 Unity 工作流](Assets/README-工作流.md)**。日常编辑不需要执行模板生成工具。

## 来源与验证

角色、武器、地面 PNG 的生成提示词和原图保存在 Assets/Art。中文字体为 Noto Sans CJK SC Regular，字体目录保留来源与 OFL 许可。像素特效为本项目的程序绘制资源；音效为本地合成原创音色，生成脚本为 Tools/GeneratePixelAudio.py。

验证入口为 Tools/CombatUpgradeChecks.cs，验证记录见 [Tools/Validation.md](Tools/Validation.md)。目前角色使用静态 Sprite 的翻转、摆动和颜色反馈，没有逐帧行走动画、局外成长或移动端发布配置。
