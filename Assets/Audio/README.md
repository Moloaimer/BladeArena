# 像素音效

本项目使用 Python 标准库本地合成的 14 个原创 WAV：22050 Hz、单声道、16 位 PCM。由脉冲波、三角波和固定随机噪声合成，不来自网站或商业音效包。生成工具：`Tools/GeneratePixelAudio.py`。

Pickup 拾球；Hit 命中；Hurt 玩家受伤；Clash 剑碰敌刀；Shield 护盾；Upgrade 升级；Wave 剑气；Mana 恢复完整魔力槽；Refill E 补剑；RoundTable Q；Charge 圣剑蓄力；Holy 圣剑释放；Boss 登场；Death 死亡。

`Assets/Prefabs/Audio/GameAudio.prefab` 保存音效引用、音量及 8 个通道。高频命中 / 剑气 / 碰撞音效限制播放频率，并降低命中和剑气音量。可以直接替换 WAV 或在 Prefab 中指定新的 AudioClip。
