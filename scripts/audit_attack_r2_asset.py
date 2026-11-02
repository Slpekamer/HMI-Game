from pathlib import Path
import json,struct,hashlib
import sys
old=Path(sys.argv[1]);new=Path(sys.argv[2])
def load(p):
 raw=p.read_bytes();off=12;chunks={}
 while off<len(raw):
  n,kind=struct.unpack_from('<II',raw,off);chunks[kind]=raw[off+8:off+8+n];off+=8+n
 return json.loads(chunks[0x4e4f534a]),chunks[0x004e4942]
a,ab=load(old);b,bb=load(new);assert bb[:len(ab)]==ab
for k in set(a)-{'buffers','bufferViews','accessors','animations'}:assert a[k]==b[k],k
assert b['accessors'][:len(a['accessors'])]==a['accessors'];assert b['bufferViews'][:len(a['bufferViews'])]==a['bufferViews']
def times(j,bin,idx):
 ac=j['accessors'][idx];view=j['bufferViews'][ac['bufferView']];offset=view.get('byteOffset',0)+ac.get('byteOffset',0);assert ac['type']=='SCALAR' and ac['componentType']==5126
 return [struct.unpack_from('<f',bin,offset+i*view.get('byteStride',4))[0] for i in range(ac['count'])]
allowed={f'{p}_{s}' for p in ['UpperArm','Forearm','Hand'] for s in ['L','R']};clips=[];changes=[]
for aa,ba in zip(a['animations'],b['animations']):
 assert aa['name']==ba['name'] and aa['channels']==ba['channels'];assert len(aa['samplers'])==len(ba['samplers']);lengthA=max(max(times(a,ab,s['input'])) for s in aa['samplers']);lengthB=max(max(times(b,bb,s['input'])) for s in ba['samplers']);assert lengthA==lengthB
 clips.append({'name':aa['name'],'duration':lengthA,'channels':len(aa['channels'])})
 for ch in aa['channels']:
  node=a['nodes'][ch['target']['node']]['name'];path=ch['target']['path'];sa=aa['samplers'][ch['sampler']];sb=ba['samplers'][ch['sampler']]
  if sa!=sb:
   assert node in allowed and path=='rotation';assert sa.get('interpolation')==sb.get('interpolation');ta=times(a,ab,sa['input']);tb=times(b,bb,sb['input']);assert ta[0]==tb[0] and ta[-1]==tb[-1]
   changes.append({'clip':aa['name'],'bone':node,'path':path,'oldSamples':len(ta),'newSamples':len(tb),'start':ta[0],'end':ta[-1],'interpolation':sa.get('interpolation','LINEAR')})
assert len(changes)==18 and len(clips)==3
report={'old':{'path':str(old),'bytes':old.stat().st_size,'sha256':hashlib.sha256(old.read_bytes()).hexdigest()},'r2':{'path':str(new),'bytes':new.stat().st_size,'sha256':hashlib.sha256(new.read_bytes()).hexdigest()},'clips':clips,'changes':changes,'originalBINPrefixExact':True,'nonAnimationJSONExact':True,'allOriginalAccessorsAndViewsExact':True,'nonArmRotationSamplersExact':True,'scope':'readonly GLB preservation/compatibility audit; not live integration or all-pose validation'}
out=Path(sys.argv[3]);out.write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
