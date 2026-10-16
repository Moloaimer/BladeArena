"""Copy authored assets into an isolated, reproducible Unity verification project."""
from pathlib import Path
import json, shutil
ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / '.validation' / 'ActiveRules'
TARGET.mkdir(parents=True, exist_ok=True)
for name in ('Art', 'Audio', 'Gameplay', 'Prefabs', 'Scenes', 'Editor'):
    source = ROOT / 'Assets' / name
    shutil.copytree(source, TARGET / 'Assets' / name, dirs_exist_ok=True)
    meta = Path(str(source) + '.meta')
    if meta.exists(): shutil.copy2(meta, TARGET / 'Assets' / (name + '.meta'))
settings = TARGET / 'Assets' / 'Settings'
settings.mkdir(exist_ok=True)
for name in ('DefaultArena.asset', 'DefaultArena.asset.meta', 'Upgrades.meta'):
    shutil.copy2(ROOT / 'Assets' / 'Settings' / name, settings / name)
shutil.copytree(ROOT / 'Assets/Settings/Upgrades', settings / 'Upgrades', dirs_exist_ok=True)
shutil.copy2(ROOT / 'Tools/CombatUpgradeChecks.cs', TARGET / 'Assets/Editor/CombatUpgradeChecks.cs')
(TARGET / 'ProjectSettings').mkdir(exist_ok=True)
for name in ('ProjectVersion.txt', 'EditorBuildSettings.asset', 'InputManager.asset', 'TagManager.asset'):
    shutil.copy2(ROOT / 'ProjectSettings' / name, TARGET / 'ProjectSettings' / name)
(TARGET / 'Packages').mkdir(exist_ok=True)
modules = ('ui', 'imgui', 'physics2d', 'imageconversion', 'audio', 'animation')
deps = {'com.unity.ugui': '1.0.0', **{'com.unity.modules.' + m: '1.0.0' for m in modules}}
(TARGET / 'Packages/manifest.json').write_text(json.dumps({'dependencies': deps}), encoding='utf-8')
package = ROOT / 'Library/PackageCache/com.unity.ugui@1.0.0'
if package.exists():
    shutil.copytree(package, TARGET / 'Packages/com.unity.ugui', dirs_exist_ok=True,
                    ignore=shutil.ignore_patterns('Tests', 'Tests.meta'))
print(TARGET)
