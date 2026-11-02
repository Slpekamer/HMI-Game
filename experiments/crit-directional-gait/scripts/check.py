"""Exact GLB preservation, new-clip finite/loop/ownership checks; not exact-three-clip acceptance."""
import argparse,json,struct,hashlib,math
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--base',required=True);p.add_argument('--out',required=True);args=p.parse_args();R=Path(args.out)
def load(p):
 raw=Path(p).read_bytes();n=struct.unpack_from('<I',raw,12)[0];return json.loads(raw[20:20+n]),raw[28+n:]
a,x=load(args.base);b,y=load(R/'runtime/hero_crit-directional-candidate.glb');assert x==y[:len(x)]
for k in set(a)-{'buffers','bufferViews','accessors','animations'}:assert a[k]==b[k],k
for k in ['bufferViews','accessors','animations']:assert a[k]==b[k][:len(a[k])],k
assert [v['name'] for v in b['animations'][-2:]]==['StrafeR','Backpedal'] and len(b['animations'])==5
report={'original_binary_exact':True,'original_three_animation_definitions_exact':True,'mesh_skin_material_morph_rest_nodes_exact':True,'base_sha256':hashlib.sha256(Path(args.base).read_bytes()).hexdigest(),'runtime_sha256':hashlib.sha256((R/'runtime/hero_crit-directional-candidate.glb').read_bytes()).hexdigest(),'new_clips':{},'warning':'Not game-scale acceptance. New upper tracks constant ready; compose instance-private lower seven bones only.'}
for clip in b['animations'][-2:]:
 changes=[];constants=[];loops=[];times=[]
 for ch in clip['channels']:
  sampler=clip['samplers'][ch['sampler']]
  def values(idx):
   ac=b['accessors'][idx];v=b['bufferViews'][ac['bufferView']];n={'VEC3':3,'VEC4':4,'SCALAR':1}[ac['type']];vs=struct.unpack_from('<'+'f'*(n*ac['count']),y,v.get('byteOffset',0)+ac.get('byteOffset',0));assert all(math.isfinite(v) for v in vs);return n,vs
  n,vs=values(sampler['output']);_,ts=values(sampler['input']);times+=list(ts);first=vs[:n];last=vs[-n:];err=max(abs(u-v) for u,v in zip(first,last));loops.append(err);assert err<1e-5
  name=b['nodes'][ch['target']['node']]['name'];path=ch['target']['path'];delta=max(abs(v-first[i%n]) for i,v in enumerate(vs));(changes if delta>1e-5 else constants).append([name,path,delta]);
  if path=='scale':assert max(abs(v-1) for v in vs)<2e-5
 assert {c[0] for c in changes}<=set(['Thigh_L','Shin_L','Foot_L','Thigh_R','Shin_R','Foot_R'])
 report['new_clips'][clip['name']]={'varied_channels':changes,'constant_channels':constants,'max_loop_delta':max(loops),'time_range_s':[min(times),max(times)],'finite':True,'unit_scale':True}
(R/'evidence/exact-preservation.json').write_text(json.dumps(report,indent=2));print('PRESERVATION_FINITE_LOOP_OK')
