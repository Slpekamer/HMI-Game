"""Read-only structural/timing checks; not animation or gameplay visual approval."""
import argparse,hashlib,json,struct
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('glb');a=p.parse_args();raw=Path(a.glb).read_bytes();magic,version,total=struct.unpack_from('<III',raw);assert (magic,version,total)==(0x46546c67,2,len(raw));n=struct.unpack_from('<I',raw,12)[0];j=json.loads(raw[20:20+n]);b=raw[28+n:];assert len(j['skins'])==1 and len(j['skins'][0]['joints'])==17
assert j['meshes'][0]['extras']['targetNames']==['Blink','Smile','Surprise'];assert all('bufferView'in i for i in j['images']);expected={'Attack':13,'Idle':49,'Run':25,'StrafeR':25,'Backpedal':25};assert set(x['name']for x in j['animations'])==set(expected)
for clip in j['animations']:
 name=clip['name'];assert clip['extras']['loop']==(name!='Attack');assert all(c['target']['path']!='weights'for c in clip['channels'])
 for sampler in clip['samplers']:
  ac=j['accessors'][sampler['input']];v=j['bufferViews'][ac['bufferView']];ts=struct.unpack_from('<'+'f'*ac['count'],b,v.get('byteOffset',0)+ac.get('byteOffset',0));assert ac['count']==expected[name];assert abs(ts[0]-1/30)<1e-7 and abs(ts[-1]-expected[name]/30)<1e-7
print(json.dumps({'sha256':hashlib.sha256(raw).hexdigest(),'skin_joints':17,'morphs':['Blink','Smile','Surprise'],'clips':expected,'result':'structural/timing PASS'},indent=2))
