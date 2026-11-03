"""Read-only full-frame gates for the opt-in moving-facing Run fixture."""
import json,math,pathlib,sys
root=pathlib.Path(sys.argv[1]);reports=[]
def read(p):return [json.loads(x)for x in p.read_text().splitlines()]
def upper(p):
 lower={'Pelvis','Thigh_L','Shin_L','Foot_L','Thigh_R','Shin_R','Foot_R'}
 return [[x for x in line.split('|',1)[1].split(';')if x and x.split(':',1)[0]not in lower]for line in p.read_text().splitlines()]
for b in sorted(root.glob('*-B')):
 a=b.with_name(b.name[:-1]+'A');assert a.is_dir(),a
 assert not (b/'failure.json').exists(),b
 for folder in [a,b]:
  log=(folder/'runtime.log').read_text();assert 'CONTROLLED_GAIT_MOVIE_DONE'in log,folder
  assert not any(x in log for x in ['ERROR:','FATAL','Unhandled','leak','resources still in use']),folder
 for filename in ['authority.jsonl','camera.tsv']:
  assert(a/filename).read_bytes()==(b/filename).read_bytes(),(b,filename)
 assert upper(a/'pose.tsv')==upper(b/'pose.tsv'),(b,'upper local pose/morph')
 ma=read(a/'motion.jsonl');mb=read(b/'motion.jsonl');rows=read(b/'locomotion.jsonl');skin=read(b/'sole.jsonl');boots=read(b/'boot-geometry.jsonl')
 n=json.loads((b/'summary.json').read_text())['frame'];assert n in (180,195)
 for stream in [ma,mb,rows,skin,boots]:assert len(stream)==n and [r['frame']for r in stream]==list(range(n)),(b,'truncated/reordered stream')
 for x,y in zip(ma,mb):
  x.pop('candidate');y.pop('candidate');assert x==y,(b,'timing/input/attack/speed')
 prev=None;margins=[];floor=[];drift=[];phase=[];gaps=[];overlaps=[];settled=[];turns=0
 for row,sk,geo in zip(rows,skin,boots):
  d=row['diagnostics'];g=geo['geometry'];l,r=g['boots']
  separating=max(g['lateralGapWorld'],l['lateralMin']-r['lateralMax'],r['forwardMin']-l['forwardMax'],l['forwardMin']-r['forwardMax'],r['minY']-l['maxY'],l['minY']-r['maxY'])
  gaps.append(separating)
  if separating<=0:overlaps.append(row['frame']) # AABB overlap is inconclusive, requires mesh review, never silently accepted.
  assert min(x['kneePoleAlignment']for x in g['boots'])>.999,(b,row['frame'],'anatomical knee')
  if 'feet'not in d:continue
  assert d['failure']=='',d
  assert 0<=d['turnCrouch']<=.025001,(b,row['frame'],'unbounded crouch')
  if d['effectiveMoving'] and not d['requestedMoving']:
   assert d['releaseAge']<.03 and math.sqrt(sum(x*x for x in d['renderVelocity']))>.08,(b,row['frame'],'invalid release grace')
  turns=max(turns,d['turnCount'])
  for i,f in enumerate(d['feet']):
   margins.append(f['reachMargin']);floor.extend(p[1]-d['groundY']for p in sk['feet'][i]['points'])
   if prev:
    old,olds=prev
    if f['stance']and old['feet'][i]['stance']:
     assert math.dist(f['anchor'],old['feet'][i]['anchor'])<1e-6,(b,row['frame'],'reanchored support')
     drift.append(max(math.dist(x,y)for x,y in zip(sk['feet'][i]['points'],olds['feet'][i]['points'])))
  if prev:
   old,_=prev;phase.append(abs((d['distance']-old['distance'])-math.dist(d['root'],old['root'])))
  if d['stopAge']>0 and all(f['state']=='settled-anchor'for f in d['feet']):settled.append((row['frame'],d['stopAge']))
  prev=(d,sk)
 assert min(margins)>=.001,(b,'reach',min(margins))
 assert min(floor)>=-.001,(b,'floor',min(floor))
 assert max(drift,default=0)<.001,(b,'stance drift',max(drift))
 assert max(phase,default=0)<1e-5,(b,'actual distance')
 assert settled and settled[0][1]<=.200001,(b,'settle')
 if b.name.startswith('long-'):
  assert turns==1 and max(x['diagnostics'].get('footfalls',0)for x in rows)>=1,(b,'repeated gaps reset real travel phase')
 reports.append({'case':b.name,'frames':n,'turns':turns,'minimum_reach_world':min(margins),'minimum_sole_floor':min(floor),'maximum_continuous_stance_drift':max(drift,default=0),'maximum_distance_error':max(phase,default=0),'first_settled':settled[0],'boot_separation_minimum':min(gaps),'boot_aabb_overlap_requires_mesh_review':overlaps})
assert reports,'No complete A/B cases'
print(json.dumps({'scope':'CPU frame/authority/contact gates. AABB overlap unresolved until mesh review; not native appearance acceptance.','cases':reports},indent=2))
