import json,struct,pathlib,copy,hashlib,argparse
p=argparse.ArgumentParser();p.add_argument("--base",required=True);p.add_argument("--out",required=True);args=p.parse_args()
R=pathlib.Path(args.out)
def load(p):
 b=p.read_bytes();n=struct.unpack_from('<I',b,12)[0];return json.loads(b[20:20+n]),b[28+n:]
j,b=load(pathlib.Path(args.base));k,c=load(R/'runtime/carrier.glb');out=bytearray(b);memo={}
def copyacc(idx):
 if idx in memo:return memo[idx]
 ac=copy.deepcopy(k['accessors'][idx]);assert 'sparse' not in ac;v=k['bufferViews'][ac['bufferView']];assert 'byteStride' not in v;count={'SCALAR':1,'VEC3':3,'VEC4':4}[ac['type']];assert ac['componentType']==5126;start=v.get('byteOffset',0)+ac.get('byteOffset',0);data=c[start:start+count*4*ac['count']];out.extend(b'\0'*((-len(out))%4));ac['bufferView']=len(j['bufferViews']);ac.pop('byteOffset',None);j['bufferViews'].append({'buffer':0,'byteOffset':len(out),'byteLength':len(data)});out.extend(data);memo[idx]=len(j['accessors']);j['accessors'].append(ac);return memo[idx]
for name in ['StrafeR','Backpedal']:
 a=copy.deepcopy(next(a for a in k['animations'] if a['name']==name))
 for sampler in a['samplers']:sampler['input']=copyacc(sampler['input']);sampler['output']=copyacc(sampler['output'])
 for ch in a['channels']:
  node_name=k['nodes'][ch['target']['node']]['name'];ch['target']['node']=next(i for i,n in enumerate(j['nodes']) if n.get('name')==node_name)
 j['animations'].append(a)
j['buffers'][0]['byteLength']=len(out);js=json.dumps(j,separators=(',',':')).encode();js+=b' '*((-len(js))%4);out.extend(b'\0'*((-len(out))%4));raw=struct.pack('<III',0x46546c67,2,28+len(js)+len(out))+struct.pack('<II',len(js),0x4e4f534a)+js+struct.pack('<II',len(out),0x004e4942)+out;p=R/'runtime/hero_crit-directional-candidate.glb';p.write_bytes(raw);assert bytes(out[:len(b)])==b
json.dump({'sha256':hashlib.sha256(raw).hexdigest(),'old_binary_prefix_exact':True,'old_clips_json_exact':True,'new_clips':['StrafeR','Backpedal'],'note':'New clip has constant ready upper tracks for isolated playback; seven lower-body bones are locomotion layer. Do not treat old three-clip validator as accepting this five-clip diagnostic.'},open(R/'evidence/append.json','w'),indent=2);print(p,hashlib.sha256(raw).hexdigest())
