"""Validate authored UI SVG release bytes; does not claim visual approval."""
from pathlib import Path
import hashlib,json,struct,xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[2]/'content/ui/revision'
m=json.loads((root/'manifest.json').read_text())
for entry in m['files']:
 p=root/entry['path'];b=p.read_bytes();assert hashlib.sha256(b).hexdigest()==entry['sha256'],p
 svg=ET.fromstring(b);assert svg.tag.endswith('svg') and 'viewBox' in svg.attrib,p
 for node in svg.iter():
  assert not node.tag.endswith(('script','foreignObject','image')),p
  assert not any('href' in k for k in node.attrib),p
assert {e['path'] for e in m['files']}=={str(p.relative_to(root)) for p in root.rglob('*.svg')}
print('PASS authored UI SVGs:',len(m['files']),'hashes, XML, no external resources')

for entry in m.get('raster_files',[]):
 p=root/entry['path'];b=p.read_bytes();assert hashlib.sha256(b).hexdigest()==entry['sha256'],p
 assert b[:8]==b'\x89PNG\r\n\x1a\n';assert struct.unpack('>II',b[16:24])==(entry['width'],entry['height']),p
 assert (root/entry['provenance']).exists()
print('PASS raster UI assets:',len(m.get('raster_files',[])),'hashes, PNG dimensions, provenance')
