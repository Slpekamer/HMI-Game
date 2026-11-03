#!/usr/bin/env python3
"""Compare completed aim candidates with identical capture-only runs. No visual verdict."""
import json,pathlib,sys,math
base,candidate=map(pathlib.Path,sys.argv[1:3]); reports=[]
for b in sorted(candidate.glob('*-B')):
 a=base/b.name
 assert (b/'summary.json').exists() and not (b/'failure.json').exists(),b
 for f in ['authority.jsonl','camera.tsv','sole.jsonl','motion.jsonl','locomotion.jsonl','boot-geometry.jsonl']:
  assert(a/f).read_bytes()==(b/f).read_bytes(),(b,f)
 log=(b/'runtime.log').read_text();assert 'CONTROLLED_GAIT_MOVIE_DONE'in log and not any(x in log for x in ['ERROR:','FATAL','Unhandled','leak','resources still in use'])
 rows=[json.loads(x)for x in(b/'launch-aim.jsonl').read_text().splitlines()];assert len(rows)==180 and [x['frame']for x in rows]==list(range(180))
 ap=(a/'pose.tsv').read_text().splitlines();bp=(b/'pose.tsv').read_text().splitlines();assert len(ap)==len(bp)==180
 permitted={'Chest','UpperArm_L','UpperArm_R'};changes=0
 for x,y in zip(ap,bp):
  xx=x.split('|',1)[1].split(';');yy=y.split('|',1)[1].split(';');assert len(xx)==len(yy)
  for before,after in zip(xx,yy):
   name=before.split(':')[0]
   if name not in permitted:assert before==after,(b,name)
   elif before!=after:
    changes+=1;assert before.split('/')[0]==after.split('/')[0],(b,'bone translation',name)
 assert changes>0,(b,'no pose edit')
 active=[x['pose']for x in rows if x.get('pose',{}).get('state')=='upper-yaw-wip'];assert active
 for x in active:assert abs(x['chestYaw'])<=math.pi/3+1e-6 and 0<x['weight']<=1
 report={'case':b.name,'frames':180,'changedChestArmPoses':changes,'activeFrames':len(active),'maximumReachProxyErrorDegrees':max(x['reachHeadingError']for x in active)*180/math.pi,'settledWeightProxyErrorsDegrees':[round(x['reachHeadingError']*180/math.pi,3)for x in active if x['weight']>.999],'scope':'Numerical invariants only; proxy is not muzzle direction. Mesh and native review separate.'};reports.append(report)
assert reports
print(json.dumps(reports,indent=2))
