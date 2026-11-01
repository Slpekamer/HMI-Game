"""Check shipped cmap; operating-system fallback is not a coverage pass."""
from pathlib import Path
from fontTools.ttLib import TTFont
import hashlib,json
root=Path(__file__).resolve().parents[2];font=root/'content/fonts/CandySans.otf'
text=''.join(p.read_text(encoding='utf-8') for p in sorted((root/'src/Adapters/Godot').glob('*.cs')))+json.dumps(json.loads((root/'content/gameplay.json').read_text()),ensure_ascii=False)
extra=root/'content/fonts/runtime-text-extra.txt'
if extra.exists():text+=extra.read_text(encoding='utf-8')
required={ord(c) for c in text if not c.isspace()};cmap=set(TTFont(font).getBestCmap());missing=sorted(required-cmap)
print(json.dumps({'font_sha256':hashlib.sha256(font.read_bytes()).hexdigest(),'required_codepoints':len(required),'mapped_codepoints':len(cmap),'missing':[{'codepoint':f'U+{c:04X}','character':chr(c)} for c in missing]},ensure_ascii=False))
if missing:raise SystemExit('FAIL shipped font missing runtime characters; desktop fallback is not a pass')
manifest=root/'content/fonts/glyph-coverage.json'
if manifest.exists():
 m=json.loads(manifest.read_text());assert set(m['preserved_codepoints'])<=cmap;assert m['font_sha256']==hashlib.sha256(font.read_bytes()).hexdigest()
print('RUNTIME_FONT_GLYPHS_PASS all adapter/catalog characters and preserved subset codepoints')
