"""Dependency-free GLB/SVG release checks. Usage: python validate_assets.py OUTPUT_ROOT"""
import json,struct,hashlib,sys,xml.etree.ElementTree as ET
from pathlib import Path
root=Path(sys.argv[1]); manifest=root/'manifest.json'
if not manifest.exists(): manifest=root/'models/manifest.json'
m=json.loads(manifest.read_text());checks=[]
for a in m['assets']:
 if a.get('animation'): continue  # Independently validated by hierarchy-aware validator.
 p=root/'models'/(a['id']+'.glb');data=p.read_bytes();magic,version,length=struct.unpack_from('<III',data)
 assert magic==0x46546C67 and version==2 and length==len(data),p
 n,kind=struct.unpack_from('<II',data,12);assert kind==0x4E4F534A
 j=json.loads(data[20:20+n]);assert hashlib.sha256(data).hexdigest()==a['sha256'],p
 assert not j.get('skins') and not j.get('animations') and not j.get('extensionsRequired'),p
 assert len(j['nodes'])==1 and 'mesh' in j['nodes'][0],p
 assert not any(k in j['nodes'][0] for k in ['translation','rotation','scale','matrix']),p
 for mat in j['materials']:
  assert mat.get('alphaMode','OPAQUE')=='OPAQUE',p
  assert not mat.get('extensions'),p
  assert len(mat['pbrMetallicRoughness'].get('baseColorFactor',[1,1,1,1]))==4,p
 heights=[];floors=[];triangles=0
 for mesh in j['meshes']:
  for prim in mesh['primitives']:
   assert prim.get('mode',4)==4
   acc=j['accessors'][prim['attributes']['POSITION']];heights.append(acc['max'][1]);floors.append(acc['min'][1]);triangles+=j['accessors'][prim['indices']]['count']//3
 assert abs(min(floors))<1e-5 and abs(max(heights)-a['height_m'])<1e-5,p
 assert triangles==a['triangles'],(p,triangles)
 checks.append({'id':a['id'],'file_bytes':len(data),'triangles':triangles,'height_y':max(heights),'opaque_pbr':True,'identity_root':True,'sha256_verified':True})
uim=json.loads((root/'ui/manifest.json').read_text())
for a in uim['files']:
 p=root/'ui'/a['name'];data=p.read_bytes();assert hashlib.sha256(data).hexdigest()==a['sha256'];svg=ET.fromstring(data)
 assert svg.attrib['viewBox']=='0 0 128 128'
 assert not any(e.tag.endswith('text') or e.tag.endswith('image') for e in svg.iter()),p
json.dump({'models':checks,'svg_count':len(uim['files']),'all_passed':True},open(Path(sys.argv[2]) if len(sys.argv)>2 else root/'static-validation.json','w'),indent=2)
print(f'PASS: {len(checks)} GLBs, {len(uim["files"])} SVGs; pivots,dimensions,topology,opaque PBR,root transforms,hashes')
