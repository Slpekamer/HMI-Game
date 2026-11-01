"""Launcher asset/config gate; pixel composition review and actual APK checks are separate."""
from pathlib import Path
import hashlib
import json
import math
from PIL import Image

root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / 'content/ui/launcher/manifest.json').read_text())
expected = {'project': 192, 'main': 192, 'foreground': 432, 'background': 432}
results = {}
for role, side in expected.items():
    record = manifest['assets'][role]
    relative = Path(record['path'])
    assert not relative.is_absolute() and '..' not in relative.parts
    assert str(relative).startswith('content/ui/launcher/')
    path = root / relative
    assert hashlib.sha256(path.read_bytes()).hexdigest() == record['sha256'], role + ' hash'
    with Image.open(path) as source:
        assert source.format == 'PNG' and source.mode == 'RGBA', role + ' PNG RGBA'
        assert source.size == tuple(record['size']), role + ' declared exact dimensions'
        assert source.width == source.height and source.width >= side, role + ' platform minimum dimensions'
        side = source.width
        image = source.copy()
    alpha = image.getchannel('A')
    if role == 'foreground':
        assert alpha.getextrema() == (0, 255), 'foreground contains actual transparency and opaque face'
        assert all(alpha.getpixel((x, y)) <= 1 for x in range(side) for y in (0, side - 1))
        assert all(alpha.getpixel((x, y)) <= 1 for y in range(side) for x in (0, side - 1))
        significant = [(x, y) for y in range(side) for x in range(side) if alpha.getpixel((x, y)) >= 128]
        assert len(significant) >= side * side * .01, 'not an empty/tiny foreground'
        radius = max(math.hypot(x - (side - 1) / 2, y - (side - 1) / 2) for x, y in significant)
        assert radius <= side * 132 / 432, 'opaque face must fit central 264px Android safe circle'
        results['foreground_safe_radius_pixels'] = radius
    if role == 'background':
        assert alpha.getextrema() == (255, 255), 'adaptive background must be opaque'
        assert all(lo == hi for lo, hi in image.getextrema()), 'background is a simple solid UI color'
    results[role] = {'size': image.size, 'alpha_range': alpha.getextrema(), 'sha256': record['sha256']}
project = (root / 'project.godot').read_text()
assert 'config/icon="res://' + manifest['assets']['project']['path'] + '"' in project
presets = (root / 'export_presets.cfg').read_text()
for role, setting in [('main', 'main_192x192'), ('foreground', 'adaptive_foreground_432x432'), ('background', 'adaptive_background_432x432')]:
    assert 'launcher_icons/' + setting + '="res://' + manifest['assets'][role]['path'] + '"' in presets
assert 'keystore/' not in presets, 'private signing configuration must not be checked into public source preset'
print('LAUNCHER_ICON_PASS ' + json.dumps(results, ensure_ascii=False))
print('Scope: exact PNG dimensions/alpha/safe-circle/config references; not all launcher masks, devices, or visual approval.')
