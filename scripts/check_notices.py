"""Verify preserved runtime/font notices and explicit export inclusion."""
from pathlib import Path
import hashlib
import json
root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / 'content/licenses/manifest.json').read_text())
seen = set()
for item in manifest:
    path = item['path']
    assert path not in seen, path
    seen.add(path)
    data = (root / path).read_bytes()
    assert len(data) == item['bytes'], path
    assert hashlib.sha256(data).hexdigest() == item['sha256'], path
expected_files = {'content/fonts/LICENSE-Noto.txt'} | {str(p.relative_to(root)) for p in (root / 'content/licenses').iterdir() if p.is_file() and p.name != 'manifest.json'}
assert seen == expected_files, 'Unmanifested or missing notice file'
assert 'content/fonts/LICENSE-Noto.txt' in seen
font = (root / 'content/fonts/LICENSE-Noto.txt').read_text()
assert 'SIL OPEN FONT LICENSE' in font.upper() and 'copyright notice and this license' in font
for preset in ('export_presets.cfg', 'docs/android-debug-export-presets.cfg'):
    text = (root / preset).read_text()
    if 'android-debug' in preset:
        assert 'gradle_build/compress_native_libraries=false' in text
    for line in text.splitlines():
        assert not line.startswith(('keystore/', 'encryption/key', 'script_encryption_key')), preset
        if line.startswith('include_filter='):
            assert 'content/fonts/LICENSE-Noto.txt' in line and 'content/licenses/*' in line, preset
print(f'PASS {len(manifest)} preserved notice files and Linux/Android inclusion filters')
