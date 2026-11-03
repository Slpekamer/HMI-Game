"""Independent measured skin/authority gate for bounded fixed-heading reversals."""
import json,math,pathlib,sys
root=pathlib.Path(sys.argv[1]);reports=[]
def read(p):return [json.loads(x)for x in p.read_text().splitlines()]
lower={'Pelvis','Thigh_L','Shin_L','Foot_L','Thigh_R','Shin_R','Foot_R'}
def upper(p):return [[x for x in row.split('|',1)[1].split(';')if x and x.split(':',1)[0]not in lower]for row in p.read_text().splitlines()]
for b in sorted(root.glob('*-B')):
 name=b.name[:-2];switch=name;a=root/(name+'-A')
 for p in [a,b]:assert not(p/'failure.json').exists(),str(p)
 for filename in ['authority.jsonl','camera.tsv']:assert(a/filename).read_bytes()==(b/filename).read_bytes(),(switch,filename)
 assert upper(a/'pose.tsv')==upper(b/'pose.tsv'),(switch,'upper local pose/morph changed')
 ma=read(a/'motion.jsonl');mb=read(b/'motion.jsonl');assert len(ma)==len(mb)==180
 for x,y in zip(ma,mb):
  x.pop('candidate');y.pop('candidate');assert x==y,(switch,'motion/event parity')
  if x['input']:assert abs(math.hypot(*x['velocity'])-4.8)<.0001,(switch,'authority speed/collision')
  assert x['haste']==0,(switch,'haste pickup')
 rows=read(b/'locomotion.jsonl');skin=read(b/'sole.jsonl');boots=read(b/'boot-geometry.jsonl')
 assert len(rows)==len(skin)==len(boots)==180,(switch,'truncated geometry trace')
 anchors={};margins=[];floor=[];drift=[];flips=[];expected_flips=[];reference_axis=None;last_sign=1;previous=None;previous_skin=None;settled=[]
 for index,(row,sk,g) in enumerate(zip(rows,skin,boots)):
  assert row['frame']==sk['frame']==g['frame']==index
  assert g['geometry']['lateralGapWorld']>0,(switch,row['frame'],'boot separation')
  assert max(f['flatBasisError']for f in g['geometry']['boots'])<.0001
  assert min(f['kneePoleAlignment']for f in g['geometry']['boots'])>.999
  d=row['diagnostics']
  if 'feet'not in d:continue
  assert d['failure']=='' and d['sharedPhase']
  l,r=d['feet'];assert l['stance']==r['stance'] and abs(l['phase']-r['phase'])<.00001
  for i,f in enumerate(d['feet']):
   margins.append(f['reachMargin']);floor.extend(p[1]-d['groundY']for p in sk['feet'][i]['points'])
   if f['stance']:
    key=f['side'],tuple(f['anchor']);points=sk['feet'][i]['points'];orig=anchors.setdefault(key,points)
    drift.append(max(math.dist(x,y)for x,y in zip(points,orig)))
  if reference_axis is None:reference_axis=d['travelAxis']
  if previous:
   delta=[x-y for x,y in zip(d['root'],previous['root'])]
   signed=sum(x*y for x,y in zip(delta,reference_axis))
   if abs(signed)>.00001:
    sign=1 if signed>0 else -1
    if sign!=last_sign:expected_flips.append(row['frame'])
    last_sign=sign
   for i,(foot,oldfoot) in enumerate(zip(d['feet'],previous['feet'])):
    if foot['stance'] and oldfoot['stance']:
     assert math.dist(foot['anchor'],oldfoot['anchor'])<.00001,(switch,row['frame'],'consecutive stance anchor changed')
     assert max(math.dist(x,y)for x,y in zip(sk['feet'][i]['points'],previous_skin['feet'][i]['points']))<.00001,(switch,row['frame'],'consecutive stance actual sole moved')
   assert abs((d['distance']-previous['distance'])-math.sqrt(sum(x*x for x in delta)))<.00001
   assert sum(x*y for x,y in zip(delta,d['travelAxis']))>=-.00001,(switch,'axis opposes actual travel')
   if sum(x*y for x,y in zip(d['travelAxis'],previous['travelAxis']))<0:
    assert sum(x*y for x,y in zip(delta,d['travelAxis']))>0,(switch,'reversed before actual render motion')
    flips.append(row['frame'])
  if d['stopAge']>0 and all(f['state']=='settled-anchor'for f in d['feet']):settled.append(d['stopAge'])
  previous=d;previous_skin=sk
 assert flips==expected_flips,(switch,'actual root sign/axis flip mismatch',expected_flips,flips)
 if name.startswith('reverse-'):assert len(flips)==1,(switch,flips)
 assert min(margins)>=.001,(switch,'reach reserve',min(margins))
 assert min(floor)>=-.001 and max(drift)<=.001,(switch,'sole ground/drift')
 assert settled and min(settled)<=.200001,(switch,'late settle',settled[:1])
 reports.append({'case':switch,'actual_axis_reverse_frames':flips,'minimum_reach_margin_world':min(margins),'minimum_actual_skin_floor_height':min(floor),'max_actual_skin_anchor_drift':max(drift),'first_settle_age':min(settled)})
assert reports,'No completed candidate cases'
print(json.dumps({'scope':'Bounded fixed-body-heading reversal fixtures only; native appearance and arbitrary direction control not certified','cases':reports},indent=2))
