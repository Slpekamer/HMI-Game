"""Rebuild renamed OFL Chinese font, preserving prior cmap and adding runtime text."""
from pathlib import Path
from fontTools import subset
from fontTools.ttLib import TTFont
import hashlib,json
root=Path(__file__).resolve().parents[2];output=root/'content/fonts/CandySans.otf';source_path=Path('/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc')
prior=set(TTFont(output).getBestCmap()) if output.exists() else set()
text=''.join(p.read_text(encoding='utf-8') for p in sorted((root/'src/Adapters/Godot').glob('*.cs')))+json.dumps(json.loads((root/'content/gameplay.json').read_text()),ensure_ascii=False)
extra=root/'content/fonts/runtime-text-extra.txt'
if extra.exists():text+=extra.read_text(encoding='utf-8')
required={ord(c) for c in text if not c.isspace()}|set(range(32,127));font=TTFont(source_path,fontNumber=2)
missing=(prior|required)-set(font.getBestCmap())
if missing:raise ValueError('Official source lacks requested glyphs: '+repr(sorted(missing)))
worker=subset.Subsetter(options=subset.Options());worker.populate(unicodes=sorted(prior|required));worker.subset(font)
for record in font['name'].names:
 if record.nameID in (1,2,3,4,6):
  name='CandySans-Regular' if record.nameID==6 else ('Regular' if record.nameID==2 else 'Candy Sans');record.string=name.encode(record.getEncoding())
font.save(output)
manifest={'source':'Noto Sans CJK SC Regular, installed official Noto TTC fontNumber=2','source_sha256':hashlib.sha256(source_path.read_bytes()).hexdigest(),'license':'LICENSE-Noto.txt','font_sha256':hashlib.sha256(output.read_bytes()).hexdigest(),'preserved_codepoints':sorted(prior),'required_runtime_codepoints':sorted(required),'added_codepoints':sorted(required-prior)}
(root/'content/fonts/glyph-coverage.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
print('Wrote renamed CandySans; retained',len(prior),'codepoints; added',len(required-prior),'runtime codepoints')
