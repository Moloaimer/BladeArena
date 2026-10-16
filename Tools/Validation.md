# 验证记录

2026-10-02，Unity 2022.3.41f1c1。当前规则为 27 项强化、75 / 20 / 5 权重以及魔力驱动的 E / Q / R。

## 实际运行

独立 Unity 验证工程导入正式项目的场景、Prefab、图片、声音、强化资源和脚本，进入 Play Mode 后通过 **86 项检查**。完整结果在 `combat-upgrade-checks.txt`，测试入口为 `CombatUpgradeChecks.cs`。

覆盖范围：

- 初始数值、无上限容量、不赠送剑、平方根成长、生命恢复、尺寸与判定同步、暴击溢出和重复刷新。
- 持续接触去重、再次进入、敌刀正常命中后保留、无敌抵挡后销毁、王之财宝同样规则、双方碎剑立即移出判定。
- 敌人变红、反馈结束恢复原色、没有受击无敌或普通受击击退。
- 攻击力 × 技能倍率 × 加成 × 暴击；直接伤害与技能伤害适用范围、连击、破甲以及黄色剑气不暴击。
- 三圈首次条件、碎剑后保持三圈、超过 30 把只填外圈；剑气满容量生成、溢出数量、单次命中、重选目标、到期销毁及不递归触发。
- 蓄魔唯一、初始空槽、100 秒一格、击杀恢复、容量限制、E 不浪费、暂停和 Tab 冻结。
- Q 消耗与持续时间、半径 / 转速增益、不规则旋转、同一次判定拦截多个敌刀、没有碎剑收益、到期还原。
- R 消耗、蓄力禁移与隐藏剑、推开敌人、仍可受伤、暂停、画面内伤害、画面外不受伤、重复升级、死亡取消。
- 护盾多层换取、湖之加护计数保留、重铸返还、Avalon 消耗与冷却。
- 100 轮抽取的可选性与选项不重复；27 个已获强化槽、鼠标悬停、文字完整显示、技能按钮、暂停状态恢复。
- Boss 出现与成长、预警、击杀奖励、清除飞剑、重新加载场景后恢复初始状态。
- 14 个 AudioClip 导入引用及 8 个声音通道、保存的蓄力特效引用有效。

## 画面与音频资源

已检查 1440 × 900 的实际 Unity 截图：

- `Docs/attributes-preview.png`：27 项强化与悬停说明。
- `Docs/upgrade-preview.png`：完整三选一效果和下一级收益。
- `Docs/round-table-preview.png`：Q 剑阵与魔力 HUD。
- `Docs/charge-preview.png`：R 蓄力时隐藏剑、黄色法阵和技能栏。

独立验证工程使用内置渲染管线；截图用于检查 UI、资源和玩法表现。正式项目保留原有 URP 配置。未进行打包发布、移动端测试或极高等级的长时间性能压测。

14 个 WAV 通过格式、时长、幅值和首尾包络检查（`audio-checks.txt`）：22050 Hz、单声道、16 位 PCM，峰值 22000，未削波。本次验证没有进行扬声器听感测试。

## 重现

1. 使用 `D:/anaconda/python.exe Tools/PrepareValidationProject.py` 创建项目内的 `.validation/ActiveRules` 副本。
2. 对该独立工程启动 Unity，使用 `-batchmode -projectPath <副本绝对路径> -executeMethod CombatUpgradeChecks.AuthorAndRun -logFile <日志路径>`。
3. 不加 `-quit`：检查器会在完成或失败后自行退出。不要加 `-nographics`，否则无法输出截图。
4. 查找日志中的 `UPGRADE_CHECK ALL PASSED`。检查器修改和重载的是副本场景，不会在正式场景中保存测试状态。

`ActiveRulesBuilder.Apply` 会更新副本内的资源模板。不要在已手动调整的正式项目中反复执行模板工具。

## 正式项目导入

正式 URP 项目已运行 `ActiveRulesBuilder.Apply` 并保存 GameHUD、GeneratedWave、ReflectedWave、ExcaliburCharge、GameAudio 与 DefaultArena，Unity 批处理以 **0** 退出。首次刷新曾先使用旧脚本列表编译，随后 Unity 导入新增脚本并自动重编译成功；最终运行时与编辑器程序集均已更新。

正式资源复核：27 项强化引用、14 个 WAV 引用、技能 Prefab 引用均可解析；权重为 75 / 20 / 5，旧自动圣剑蓄能配置已移除，两个黄色剑气 Prefab 均为单次、不暴击、追踪模式。主工程日志为 `Logs/active-rules-import.log`。
