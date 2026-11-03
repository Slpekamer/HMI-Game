#!/usr/bin/env python3
"""Bounded normal-path authority, actual internal-step clocks and rigid-sole checks."""
import json,pathlib,sys,math
root=pathlib.Path(sys.argv[1]);prefix=sys.argv[2];reports=[]
def read(p):return [json.loads(x)for x in p.read_text().splitlines()]
for case in ['60','30','120','long','haste']:
 a=root/f'{prefix}-{case}-A';b=root/f'{prefix}-{case}-B'
 sa=json.loads((a/'summary.json').read_text());sb=json.loads((b/'summary.json').read_text());n=sb['frame'];assert sa['frame']==n
 for p in [a,b]:
  log=(p/'runtime.log').read_text();assert 'NORMAL_GAME_PROBE_PASS'in log and not any(x in log for x in ['ERROR:','FATAL','Unhandled','leak']),p
  assert not(p/'failure.json').exists()
 assert(a/'authority.jsonl').read_bytes()==(b/'authority.jsonl').read_bytes(),(case,'authority')
 for p in [a,b]:
  auth=read(p/'authority.jsonl');assert len(auth)==n and [r['frame']for r in auth]==list(range(n))
 ca=(a/'camera.tsv').read_text().splitlines();cb=(b/'camera.tsv').read_text().splitlines();assert len(ca)==len(cb)==n
 assert [r.split('|')[:3]for r in ca]==[r.split('|')[:3]for r in cb],(case,'camera')
 outer=read(b/'presentation.jsonl');steps=read(b/'substeps.jsonl');assert len(outer)==n and [r['frame']for r in outer]==list(range(n))
 sums={};counts={};previous=None;graph_time=0;floor=[];reach=[];drifts=[];distance_errors=[];stance_pairs=0
 for i,r in enumerate(steps):
  assert 0<=r['frame']<n and r['graphCalls']==i+1,(case,'graph count',i)
  assert 0<r['subDt']<=1/60+1e-8 and abs((r['graphSeconds']-graph_time)-r['subDt'])<1e-7,(case,'graph elapsed',i)
  graph_time=r['graphSeconds'];sums[r['frame']]=sums.get(r['frame'],0)+r['subDt'];counts[r['frame']]=counts.get(r['frame'],0)+1
  d=r['gait']
  if not d or 'feet'not in d:previous=None;continue
  assert d['failure']=='' and d['renderDistance']<=.12001,(case,'guard',i)
  for f in d['feet']:
   reach.append(f['reachMargin']);floor.extend(x[1]-d['groundY']for x in f['solePositions'])
  if previous:
   distance_errors.append(abs(d['distance']-previous['distance']-math.dist(d['root'],previous['root'])))
   for old,f in zip(previous['feet'],d['feet']):
    if old['stance']and f['stance']:
     # Consecutive bounded <=.12 m internal steps cannot skip a 2.16 m swing interval.
     assert math.dist(old['anchor'],f['anchor'])<1e-6,(case,'reanchored internal stance',i)
     drifts.extend(math.dist(x,y)for x,y in zip(old['solePositions'],f['solePositions']));stance_pairs+=1
  previous=d
 for r in outer:
  assert counts.get(r['frame'],0)==r['LastPresentationSteps'] and abs(sums.get(r['frame'],0)-r['dt'])<1e-6,(case,'render step sum',r['frame'])
 assert reach and min(reach)>=.001 and min(floor)>=-.001,(case,'reach/floor')
 assert max(drifts,default=0)<.001 and max(distance_errors,default=0)<1e-5,(case,'support/distance')
 reports.append({'case':case,'renders':n,'internalSteps':len(steps),'maxStepsPerRender':max(counts.values()),'minimumReachWorld':min(reach),'minimumSoleFloorWorld':min(floor),'continuousInternalStancePairs':stance_pairs,'maximumSoleDriftWorld':max(drifts,default=0),'maximumDistanceErrorWorld':max(distance_errors,default=0),'actualGraphSeconds':graph_time})
print(json.dumps({'scope':'Five declared normal-path cases, identical authority/camera A/B and actual post-step rigid sole/graph clocks. Internal steps are not additional rendered frames; not full mesh, native appearance, all stats or device certification.','cases':reports},indent=2))
