"""v02 hierarchy-aware glTF2 validator; standard library only.
Validates full world-space rest geometry, root/no-root-motion, actual articulated channels,
clip duration, neutral asset contract, opaque PBR, budget metrics and content hashes.
"""
import json,struct,hashlib,math,sys
from pathlib import Path
root=Path(sys.argv[1]);manifest=json.loads((root/('animated-manifest.json' if (root/'animated-manifest.json').exists() else 'manifest.json')).read_text());reports=[]
combined_path=root/'models/manifest.json'
if combined_path.exists():
 combined=json.loads(combined_path.read_text())
 declared={a['id']:a['sha256'] for a in combined['assets'] if a.get('animation')}
 actual={a['id']:a['sha256'] for a in manifest['assets']}
 assert declared==actual, 'Animated manifest must exactly cover combined release animated entries'
 assert len(actual)==8, 'Expected all five heroes, two companions and chaser'
I=[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1]
def mul(a,b):return [sum(a[r*4+k]*b[k*4+c] for k in range(4)) for r in range(4) for c in range(4)]
def transform(m,v):return [sum(m[r*4+c]*v[c] for c in range(3))+m[r*4+3] for r in range(3)]
def matrix(n):
 if 'matrix' in n:return [n['matrix'][c*4+r] for r in range(4) for c in range(4)]
 x,y,z,w=n.get('rotation',[0,0,0,1]);sx,sy,sz=n.get('scale',[1,1,1]);tx,ty,tz=n.get('translation',[0,0,0])
 return [(1-2*y*y-2*z*z)*sx,(2*x*y-2*z*w)*sy,(2*x*z+2*y*w)*sz,tx,(2*x*y+2*z*w)*sx,(1-2*x*x-2*z*z)*sy,(2*y*z-2*x*w)*sz,ty,(2*x*z-2*y*w)*sx,(2*y*z+2*x*w)*sy,(1-2*x*x-2*y*y)*sz,tz,0,0,0,1]
for a in manifest['assets']:
 if a.get('runtime_contract')=='food-hero-v1':
  from validate_food_heroes import check
  food=check(root/'models'/(a['id']+'.glb'))
  assert food['sha256']==a['sha256'] and food['triangles']==a['triangles'], 'Food runtime manifest mismatch'
  reports.append({'id':a['id'],'runtime_contract':'food-hero-v1',**food})
  continue
 path=root/'models'/(a['id']+'.glb');data=path.read_bytes();magic,version,size=struct.unpack_from('<III',data);assert (magic,version,size)==(0x46546c67,2,len(data))
 n,kind=struct.unpack_from('<II',data,12);assert kind==0x4e4f534a;j=json.loads(data[20:20+n]);binary=20+n+8
 def access(idx):
  ac=j['accessors'][idx];view=j['bufferViews'][ac['bufferView']];comps={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[ac['type']];fmt={5126:'f',5123:'H',5125:'I',5121:'B'}[ac['componentType']];unit=struct.calcsize(fmt);stride=view.get('byteStride',comps*unit);start=binary+view.get('byteOffset',0)+ac.get('byteOffset',0)
  return [struct.unpack_from('<'+fmt*comps,data,start+i*stride) for i in range(ac['count'])]
 assert not j.get('extensionsRequired') and not j.get('skins')
 roots=j['scenes'][j.get('scene',0)]['nodes'];assert len(roots)==1;ri=roots[0];assert j['nodes'][ri]['name']==a['id'];assert all(abs(x-y)<1e-6 for x,y in zip(matrix(j['nodes'][ri]),I))
 coords=[];triangles=0;surfaces=0;worlds={}
 def visit(idx,parent):
  global triangles,surfaces
  node=j['nodes'][idx];world=mul(parent,matrix(node));worlds[idx]=world
  if 'mesh' in node:
   for prim in j['meshes'][node['mesh']]['primitives']:
    assert prim.get('mode',4)==4
    coords.extend(transform(world,v) for v in access(prim['attributes']['POSITION']));triangles+=j['accessors'][prim['indices']]['count']//3;surfaces+=1
  for child in node.get('children',[]):visit(child,world)
 visit(ri,I)
 lo=[min(v[i] for v in coords) for i in range(3)];hi=[max(v[i] for v in coords) for i in range(3)]
 assert abs(lo[1])<1e-4,(a['id'],'ground',lo)
 assert abs(hi[1]-a['height_m'])<1e-4,(a['id'],'height',hi)
 for mat in j['materials']:assert mat.get('alphaMode','OPAQUE')=='OPAQUE' and not mat.get('extensions')
 assert hashlib.sha256(data).hexdigest()==a['sha256'];assert triangles==a['triangles']
 clips={}
 assert {anim['name'] for anim in j['animations']}=={'Idle','Run','Attack'}
 for anim in j['animations']:
  nodes=set();changes=set();end=0
  for ch in anim['channels']:
   node=ch['target']['node'];assert node!=ri,(a['id'],'root animation')
   nodes.add(j['nodes'][node]['name']);sm=anim['samplers'][ch['sampler']];times=access(sm['input']);end=max(end,max(v[0] for v in times));values=access(sm['output'])
   if any(max(v[c] for v in values)-min(v[c] for v in values)>.005 for c in range(len(values[0]))):changes.add(j['nodes'][node]['name'])
  expected=a['clips'][anim['name']]['duration_seconds'];assert abs(end-expected)<.001,(a['id'],anim['name'],end)
  assert len(changes)>=2,(a['id'],anim['name'],'insufficient articulated motion',changes)
  assert 'Head' in changes or any(n.startswith('Arm') or n.startswith('Foot') for n in changes)
  clips[anim['name']]={'duration_seconds':end,'loop_policy':anim['name'] in ['Idle','Run'],'track_count':len(anim['channels']),'animated_nodes':sorted(nodes),'changing_nodes':sorted(changes),'root_channels':0}
 report={'id':a['id'],'sha256':a['sha256'],'rest_world_min':lo,'rest_world_max':hi,'triangles':triangles,'meshes':len(j['meshes']),'surfaces_draw_calls_upper_bound':surfaces,'clips':clips,'ok':True};reports.append(report)
 a.update({'rest_world_min':lo,'rest_world_max':hi,'surface_count':surfaces,'clip_validation':clips})
json.dump({'all_passed':True,'assets':reports},open(Path(sys.argv[2]) if len(sys.argv)>2 else root/'animated-static-validation.json','w'),indent=2)
# Validation never mutates the release manifest.
print('PASS',len(reports),'animated GLB assets: legacy hierarchy validation or explicit food-hero static contract; actual imported skeletal motion/root checks run separately')
