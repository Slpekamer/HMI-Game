#!/usr/bin/env python3
"""Offline evaluated lower-mesh audit (numpy/scipy), not a visual acceptance test.

Usage: inspect_skin.py ASSET.glb TRACE_FOLDER --frames 0,31,32,33,40
Regions are source-height/bone-derived, NOT semantic material labels. Reports
non-coplanar surface crossings and a separate coplanar candidate count; it does
not certify absence of coplanar penetration. Static/rest intersections are kept
as baseline counterevidence rather than silently discarded. Morph displacement
is not evaluated: this asset's lower-body morph deltas must be zero.
"""
import argparse,json,struct
from pathlib import Path
import numpy as np
from scipy.spatial import cKDTree

def transform(s):
    a=np.asarray([[float(v) for v in row.split(',')] for row in s.split('/')])
    m=np.eye(4);m[:3,3]=a[0];m[:3,:3]=a[1:].T;return m

def load(path):
    b=path.read_bytes();n=struct.unpack_from('<I',b,12)[0];j=json.loads(b[20:20+n]);data=b[28+n:]
    def acc(i):
        a=j['accessors'][i];v=j['bufferViews'][a['bufferView']]
        dt=np.dtype({5126:'<f4',5123:'<u2',5121:'u1',5125:'<u4'}[a['componentType']]);k={'SCALAR':1,'VEC3':3,'VEC4':4,'MAT4':16}[a['type']]
        return np.ndarray((a['count'],k),dtype=dt,buffer=data,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',dt.itemsize*k),dt.itemsize)).copy()
    skin=j['skins'][0];nodes=skin['joints'];names=[j['nodes'][i]['name'] for i in nodes];parents={c:p for p,x in enumerate(j['nodes']) for c in x.get('children',[])}
    parent=[nodes.index(parents[i]) if parents.get(i) in nodes else -1 for i in nodes]
    binds=acc(skin['inverseBindMatrices']).reshape(-1,4,4).transpose(0,2,1)
    prim=j['meshes'][0]['primitives'][0];a=prim['attributes'];v=acc(a['POSITION']).astype(float);bones=acc(a['JOINTS_0']);weights=acc(a['WEIGHTS_0']);faces=acc(prim['indices']).reshape(-1,3)
    lower=v[:,1]<.34
    for target in prim.get('targets',[]):
        if 'POSITION' in target and np.max(np.abs(acc(target['POSITION'])[lower]))>1e-8:raise ValueError('Lower-body morph displacement requires explicit evaluation')
    return names,parent,binds,v,bones,weights,faces

def crossings(verts,faces,ids_a,ids_b):
    """Exact edge/triangle crossing tests after conservative spherical/AABB cull.
    Shared source-vertex pairs are excluded (adjacent mesh faces). Coplanar AABB
    candidates are only reported, not falsely classified as intersections.
    """
    ta=verts[faces[ids_a]];tb=verts[faces[ids_b]]
    ca=ta.mean(1);cb=tb.mean(1);ra=np.linalg.norm(ta-ca[:,None],axis=2).max(1);rb=np.linalg.norm(tb-cb[:,None],axis=2).max(1)
    tree=cKDTree(cb);pairs=[]
    for i,near in enumerate(tree.query_ball_point(ca,ra+rb.max()+1e-8)):
        for k in near:
            if np.all(ta[i].max(0)+1e-8>=tb[k].min(0)) and np.all(tb[k].max(0)+1e-8>=ta[i].min(0)) and not np.intersect1d(faces[ids_a[i]],faces[ids_b[k]]).size:pairs.append((i,k))
    if not pairs:return set(),0
    pairs=np.asarray(pairs);a=ta[pairs[:,0]];b=tb[pairs[:,1]]
    def edge_hits(x,y):
        e1=y[:,1]-y[:,0];e2=y[:,2]-y[:,0];hit=np.zeros(len(x),bool)
        for k in range(3):
            orig=x[:,k];d=x[:,(k+1)%3]-orig;p=np.cross(d,e2);det=np.einsum('ij,ij->i',e1,p);valid=np.abs(det)>1e-12;inv=np.divide(1.,det,out=np.zeros_like(det),where=valid);tvec=orig-y[:,0];u=np.einsum('ij,ij->i',tvec,p)*inv;q=np.cross(tvec,e1);v=np.einsum('ij,ij->i',d,q)*inv;t=np.einsum('ij,ij->i',e2,q)*inv
            hit|=valid&(u>=-1e-7)&(v>=-1e-7)&(u+v<=1+1e-7)&(t>=-1e-7)&(t<=1+1e-7)
        return hit
    hit=edge_hits(a,b)|edge_hits(b,a)
    na=np.cross(a[:,1]-a[:,0],a[:,2]-a[:,0]);nb=np.cross(b[:,1]-b[:,0],b[:,2]-b[:,0]);den=np.linalg.norm(na,axis=1)*np.linalg.norm(nb,axis=1)
    parallel=np.linalg.norm(np.cross(na,nb),axis=1)<=1e-6*den
    cop=parallel&(np.abs(np.einsum('ij,ij->i',na,b[:,0]-a[:,0]))<=1e-7*np.linalg.norm(na,axis=1))
    return {(int(ids_a[i]),int(ids_b[k])) for i,k in pairs[hit&~cop]},int(cop.sum())

def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('asset',type=Path);ap.add_argument('trace',type=Path);ap.add_argument('--frames',default='0,30,31,32,33,34,35,36,37,38,39,40,45,50,55,60,150,151,152,153,154,155,156,157,158,159,160,170,180,194');args=ap.parse_args()
    names,parent,binds,rest,bones,w,faces=load(args.asset);ids={n:i for i,n in enumerate(names)}
    mass=lambda n:np.sum(w*(bones==ids[n]),axis=1)
    boot={s:np.flatnonzero(np.all(mass('Foot_'+s)[faces]>.99999,axis=1)) for s in ['L','R']}
    upper=np.flatnonzero(np.all((rest[:,1]>=.17)[faces],axis=1)&np.all((rest[:,1]<=.34)[faces],axis=1)&np.all((mass('Foot_L')+mass('Foot_R'))[faces]<.001,axis=1))
    legs={s:np.flatnonzero(np.all((mass('Thigh_'+s)+mass('Shin_'+s))[faces]>.5,axis=1)) for s in ['L','R']}
    regions={'boot_L_vs_upper_lowerbody':(boot['L'],upper),'boot_R_vs_upper_lowerbody':(boot['R'],upper),'left_leg_vs_right_leg':(legs['L'],legs['R'])}
    base={k:crossings(rest,faces,*v) for k,v in regions.items()}
    poses={int(x.split('|',1)[0]):x.split('|',1)[1] for x in (args.trace/'pose.tsv').read_text().splitlines()};cameras={int(x.split('|')[0]):transform(x.split('|')[3]) for x in (args.trace/'camera.tsv').read_text().splitlines()};soles={x['frame']:x for x in map(json.loads,(args.trace/'sole.jsonl').read_text().splitlines())}
    reports=[];max_sole_error=0
    for frame in map(int,args.frames.split(',')):
        if frame not in poses:raise ValueError('Requested frame absent: '+str(frame))
        local={n:transform(t) for entry in poses[frame].split(';') if ':' in entry for n,t in [entry.split(':',1)] if n in ids};glob={}
        def g(i):
            if i not in glob:glob[i]=(g(parent[i]) if parent[i]>=0 else np.eye(4))@local[names[i]]
            return glob[i]
        skin=np.asarray([g(i)@binds[i] for i in range(len(names))]);p=np.c_[rest,np.ones(len(rest))];evaluated=np.zeros_like(p)
        for k in range(w.shape[1]):evaluated+=np.einsum('nij,nj->ni',skin[bones[:,k]],p)*w[:,k,None]
        verts=evaluated[:,:3];world=(cameras[frame]@np.c_[verts,np.ones(len(verts))].T).T[:,:3]
        sole_errors=[]
        for s in ['L','R']:
            mask=mass('Foot_'+s)>.99999;mask&=rest[:,1]<=rest[mask,1].min()+.0001
            expected=next(x for x in soles[frame]['feet'] if x['side']=='Foot_'+s)['points'];a=world[mask];b=np.asarray(expected)
            if len(a)!=len(b):raise ValueError('Sole vertex count differs')
            error=max(cKDTree(b).query(a)[0].max(),cKDTree(a).query(b)[0].max());sole_errors.append(float(error));max_sole_error=max(max_sole_error,error)
        if max(sole_errors)>1e-5:raise ValueError('Reconstructed actual skin disagrees with measured sole: '+str(sole_errors))
        row={'frame':frame,'sole_reconstruction_max_error_world':max(sole_errors),'regions':{}}
        for name,pair in regions.items():
            hits,cop=crossings(verts,faces,*pair);new=hits-base[name][0]
            row['regions'][name]={'noncoplanar_crossing_pairs':len(hits),'new_vs_asset_rest_pairs':len(new),'rest_pairs':len(base[name][0]),'coplanar_aabb_candidates_unresolved':cop,'new_pair_examples':sorted(new)[:12]}
        reports.append(row)
    print(json.dumps({'scope':'Evaluated lower-mesh LBS; no semantic shorts labels, GPU certification or aesthetic acceptance. Coplanar candidates unresolved. Rest seams retained as baseline. Samples are not continuous-time proof.','region_triangle_counts':{k:[len(a),len(b)]for k,(a,b)in regions.items()},'max_sole_reconstruction_error_world':float(max_sole_error),'frames':reports},indent=2))
if __name__=='__main__':main()
