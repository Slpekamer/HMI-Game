#!/usr/bin/env python3
"""Deterministic glTF 2.0 runtime preparation. Requires Python 3 + numpy.
Does not model, re-bake, simplify, quantize, or change animation. Input is a
Blender-exported, baked/skinned GLB. Output and JSON report are explicit paths.
"""
import argparse,copy,hashlib,json,struct
from pathlib import Path
import numpy as np
DT={5120:np.int8,5121:np.uint8,5122:np.int16,5123:np.uint16,5125:np.uint32,5126:np.float32}
N={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
class GLB:
 def __init__(self,path):
  self.raw=Path(path).read_bytes();magic,version,size=struct.unpack_from('<4sII',self.raw)
  assert magic==b'glTF' and version==2 and size==len(self.raw)
  jl,jt=struct.unpack_from('<II',self.raw,12);assert jt==0x4e4f534a
  self.d=json.loads(self.raw[20:20+jl]);bp=20+jl;bl,bt=struct.unpack_from('<II',self.raw,bp);assert bt==0x004e4942
  self.b=bytearray(self.raw[bp+8:bp+8+bl])
 def read(self,i):
  a=self.d['accessors'][i];n=N[a['type']];dt=DT[a['componentType']];out=np.zeros((a['count'],n),dtype=dt)
  if 'bufferView' in a:
   v=self.d['bufferViews'][a['bufferView']];out=np.ndarray(out.shape,dtype=dt,buffer=self.b,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',n*np.dtype(dt).itemsize),np.dtype(dt).itemsize)).copy()
  if 'sparse' in a:
   s=a['sparse'];v=self.d['bufferViews'][s['indices']['bufferView']];ids=np.frombuffer(self.b,dtype=DT[s['indices']['componentType']],count=s['count'],offset=v.get('byteOffset',0)+s['indices'].get('byteOffset',0));v=self.d['bufferViews'][s['values']['bufferView']];values=np.frombuffer(self.b,dtype=dt,count=s['count']*n,offset=v.get('byteOffset',0)+s['values'].get('byteOffset',0)).reshape(-1,n);out[ids]=values
  return out
 def view(self,data,target=None):
  self.b+=b'\0'*((-len(self.b))%4);v={'buffer':0,'byteOffset':len(self.b),'byteLength':len(data)}
  if target:v['target']=target
  i=len(self.d['bufferViews']);self.d['bufferViews'].append(v);self.b.extend(data);return i
 def store(self,a,template,target=None):
  a=np.ascontiguousarray(a);v=self.view(a.tobytes(),target);t={k:copy.deepcopy(x) for k,x in template.items() if k not in ('bufferView','byteOffset','sparse','count','min','max')};t.update(bufferView=v,count=len(a))
  if 'min' in template:t['min']=a.min(axis=0).tolist()
  if 'max' in template:t['max']=a.max(axis=0).tolist()
  i=len(self.d['accessors']);self.d['accessors'].append(t);return i
 def save(self,path):
  self.d['buffers']=[{'byteLength':len(self.b)}];j=json.dumps(self.d,separators=(',',':')).encode();j+=b' '*((-len(j))%4);b=self.b+b'\0'*((-len(self.b))%4)
  raw=struct.pack('<4sII',b'glTF',2,28+len(j)+len(b))+struct.pack('<II',len(j),0x4e4f534a)+j+struct.pack('<II',len(b),0x004e4942)+b;Path(path).parent.mkdir(parents=True,exist_ok=True);Path(path).write_bytes(raw);return hashlib.sha256(raw).hexdigest()
def split(g,tolerance,rings,prefix=None):
 """Split indexed draw subsets, retaining every original attribute byte."""
 assert len(g.d['meshes'])==1 and len(g.d['meshes'][0]['primitives'])==1
 m=g.d['meshes'][0];p=m['primitives'][0];pos=g.read(p['attributes']['POSITION']);idx=g.read(p['indices']).reshape(-1,3);moving=np.zeros(len(pos),bool)
 for target in p.get('targets',[]):moving|=np.linalg.norm(g.read(target['POSITION']),axis=1)>tolerance
 chosen=moving[idx].any(axis=1);keys=[tuple(x) for x in pos]
 for _ in range(rings):
  touched={keys[i] for i in idx[chosen].ravel()};near=np.array([k in touched for k in keys]);chosen|=near[idx].any(axis=1)
 assert chosen.any() and (~chosen).any()
 def make(mask):
  q=copy.deepcopy(p);a=idx[mask].astype(np.uint32).reshape(-1,1);q['indices']=g.store(a,{'type':'SCALAR','componentType':5125,'min':[0],'max':[0]},34963);return q
 static=make(~chosen);static.pop('targets',None);face=make(chosen);base_name=prefix or m.get('name','Hero');suffix='' if prefix else '_';g.d['meshes']=[{'name':base_name+suffix+'Static','primitives':[static]},{**copy.deepcopy(m),'name':base_name+suffix+'Face','primitives':[face]}]
 ni=next(i for i,n in enumerate(g.d['nodes']) if n.get('mesh')==0);new=copy.deepcopy(g.d['nodes'][ni]);new.update(mesh=1,name=base_name+suffix+'Face');g.d['nodes'][ni]['name']=base_name+suffix+'Static';g.d['nodes'][ni].pop('weights',None);nj=len(g.d['nodes']);g.d['nodes'].append(new)
 parents=[n for n in g.d['nodes'] if ni in n.get('children',[])]
 for n in parents:n['children'].append(nj)
 if not parents:
  for scene in g.d['scenes']:
   if ni in scene.get('nodes',[]):scene['nodes'].append(nj)
 return {'static_triangles':int((~chosen).sum()),'dynamic_triangles':int(chosen.sum()),'tolerance_m':tolerance,'padding_rings':rings}
def compact(g):
 old=copy.deepcopy(g);g.d['accessors']=[];g.d['bufferViews']=[];g.b=bytearray();cache={};stats=[]
 def whole(i):
  if i not in cache:cache[i]=g.store(old.read(i),old.d['accessors'][i])
  return cache[i]
 for m in g.d['meshes']:
  for p in m['primitives']:
   oi=p['indices'];ids=old.read(oi).ravel();used=np.unique(ids);remap=np.full(int(used.max())+1,-1,np.int32);remap[used]=np.arange(len(used))
   for name,i in list(p['attributes'].items()):p['attributes'][name]=g.store(old.read(i)[used],old.d['accessors'][i],34962)
   for target in p.get('targets',[]):
    for name,i in list(target.items()):target[name]=g.store(old.read(i)[used],old.d['accessors'][i],34962)
   t=copy.deepcopy(old.d['accessors'][oi]);t['componentType']=5125;p['indices']=g.store(remap[ids].astype(np.uint32).reshape(-1,1),t,34963);stats.append({'name':m.get('name'),'vertices':len(used),'triangles':len(ids)//3})
 for skin in g.d.get('skins',[]):
  if 'inverseBindMatrices' in skin:skin['inverseBindMatrices']=whole(skin['inverseBindMatrices'])
 for animation in g.d.get('animations',[]):
  for s in animation['samplers']:
   for k in ('input','output'):s[k]=whole(s[k])
 for image in g.d.get('images',[]):
  if 'bufferView' in image:
   v=old.d['bufferViews'][image['bufferView']];start=v.get('byteOffset',0);image['bufferView']=g.view(old.b[start:start+v['byteLength']])
 return stats
def repair(g):
 changes=[]
 for mi,m in enumerate(g.d['meshes']):
  for p in m['primitives']:
   if 'TANGENT' not in p['attributes']:raise ValueError('Export tangents in Blender before processing')
   pos=g.read(p['attributes']['POSITION']).astype('float64');uv=g.read(p['attributes']['TEXCOORD_0']).astype('float64');ns=g.read(p['attributes']['NORMAL']).astype('float64');ai=p['attributes']['TANGENT'];ts=g.read(ai);ids=g.read(p['indices']).reshape(-1,3)
   for i in np.flatnonzero(np.linalg.norm(ts[:,:3],axis=1)<1e-8):
    incident=np.flatnonzero((ids==i).any(axis=1));assert len(incident)==1,'Ambiguous shared invalid tangent: inspect rather than invent'
    a,b,c=ids[incident[0]];e1=pos[b]-pos[a];e2=pos[c]-pos[a];u1=uv[b]-uv[a];u2=uv[c]-uv[a];det=u1[0]*u2[1]-u1[1]*u2[0];assert abs(det)>1e-14,'Degenerate UV: rebake, do not fabricate tangent'
    T=(e1*u2[1]-e2*u1[1])/det;B=(e2*u1[0]-e1*u2[0])/det;n=ns[i]/np.linalg.norm(ns[i]);T-=n*np.dot(T,n);assert np.linalg.norm(T)>1e-8;T/=np.linalg.norm(T);w=1. if np.dot(np.cross(n,T),B)>=0 else -1.;value=np.array([*T,w],np.float32)
    ac=g.d['accessors'][ai];v=g.d['bufferViews'][ac['bufferView']];offset=v.get('byteOffset',0)+ac.get('byteOffset',0)+int(i)*v.get('byteStride',16);g.b[offset:offset+16]=value.tobytes();changes.append({'mesh':mi,'vertex':int(i),'old':ts[i].tolist(),'new':value.tolist(),'position':pos[i].tolist(),'uv_determinant':float(det)})
 return changes
def main():
 p=argparse.ArgumentParser();p.add_argument('input',type=Path);p.add_argument('output',type=Path);p.add_argument('--report',required=True,type=Path);p.add_argument('--split-local-morph',action='store_true');p.add_argument('--tolerance',type=float,default=1e-6);p.add_argument('--rings',type=int,default=2);p.add_argument('--mesh-prefix');p.add_argument('--repair-zero-tangents',action='store_true');a=p.parse_args();g=GLB(a.input);report={'input':str(a.input),'input_sha256':hashlib.sha256(g.raw).hexdigest(),'scope':'Deterministic runtime derivative; actual engine visual/performance validation separate'}
 if a.split_local_morph:report['split']=split(g,a.tolerance,a.rings,a.mesh_prefix)
 report['compaction']=compact(g)
 if a.repair_zero_tangents:report['tangent_repairs']=repair(g)
 report['output_sha256']=g.save(a.output);a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
if __name__=='__main__':main()
