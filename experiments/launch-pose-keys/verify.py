#!/usr/bin/env python3
"""Scoped numerical and trigger invariants; actual skin/visual checks remain separate."""
import json,pathlib,sys,math
root=pathlib.Path(sys.argv[1]);reports=[]
paired=not(root/'baseline').is_dir()
cases=sorted(root.glob('*-B')) if paired else sorted(p for p in root.iterdir() if p.is_dir() and p.name!='baseline')
allowed={'Chest','UpperArm_L','Forearm_L','Hand_L','UpperArm_R','Forearm_R','Hand_R'}
for b in cases:
 a=b.with_name(b.name[:-1]+'A') if paired else root/'baseline'
 n=json.loads((b/'summary.json').read_text())['frame'];assert n in (180,195,210)
 for f in ['authority.jsonl','camera.tsv','sole.jsonl','motion.jsonl','locomotion.jsonl','boot-geometry.jsonl']:assert(a/f).read_bytes()==(b/f).read_bytes(),(b,f)
 for p in [a,b]:
  log=(p/'runtime.log').read_text();assert 'CONTROLLED_GAIT_MOVIE_DONE'in log and not any(x in log for x in ['ERROR:','FATAL','Unhandled','resources still in use','leak'])
  assert not(p/'failure.json').exists()
 aa=(a/'pose.tsv').read_text().splitlines();bb=(b/'pose.tsv').read_text().splitlines();assert len(aa)==len(bb)==n;changes=0
 for x,y in zip(aa,bb):
  xx=x.split('|',1)[1].split(';');yy=y.split('|',1)[1].split(';');assert len(xx)==len(yy)
  for u,v in zip(xx,yy):
   name=u.split(':')[0]
   if name not in allowed:assert u==v,(b,name)
   elif u!=v:
    changes+=1;assert u.split('/')[0]==v.split('/')[0],(b,'bone translation',name)
    for c,d in zip(u.split('/')[1:],v.split('/')[1:]):
     assert abs(sum(float(x)**2 for x in c.split(','))-sum(float(x)**2 for x in d.split(',')))<3e-6,(b,'bone scale',name)
 rows=[json.loads(x)for x in(b/'launch-aim.jsonl').read_text().splitlines()];auth=[json.loads(x)for x in(b/'authority.jsonl').read_text().splitlines()]
 assert len(rows)==n and [x['frame']for x in rows]==list(range(n));starts=[];previous=None;active=0;cancel=0
 for row,authority in zip(rows,auth):
  pose=row['launchPose'];state=pose['state']
  if state=='launch-key-wip':
   active+=1;assert 0<pose['age']<=.50+1/60+1e-6
   if previous!=pose['Tick']:
    sample=row['sample'];assert sample and sample['Tick']==pose['Tick']
    events=[x for x in authority['transitions']if x['Kind']==0 and x['ProjectileKind']==0 and x['Id']==sample['Projectile'] and x['Tick']==sample['Tick'] and x['Owner']==sample['Owner']]
    assert len(events)==1,(b,'pose without real matching launch',row['frame']);starts.append(row['frame']);previous=pose['Tick']
  elif state=='cancel-to-original-gesture':cancel+=1;assert pose['cancelAge']<=.06+1/60+1e-6
 assert starts and changes>0
 reports.append({'case':b.name,'frames':n,'realLaunchStarts':starts,'activeFrames':active,'cancelFrames':cancel,'changedUpperLocalPoses':changes,'scope':'Root, feet, authority, original attack clocks and other bones unchanged. Not a mesh or appearance verdict.'})
assert reports
print(json.dumps(reports,indent=2))
