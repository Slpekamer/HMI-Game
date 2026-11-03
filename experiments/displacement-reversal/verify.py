"""Independent measured skin/authority gate for bounded fixed-heading reversals."""
import json,math,pathlib,sys
root=pathlib.Path(sys.argv[1]);reports=[]
def read(p):return [json.loads(x)for x in p.read_text().splitlines()]
lower={'Pelvis','Thigh_L','Shin_L','Foot_L','Thigh_R','Shin_R','Foot_R'}
def upper(p):return [[x for x in row.split('|',1)[1].split(';')if x and x.split(':',1)[0]not in lower]for row in p.read_text().splitlines()]
for switch in range(60,75):
 a=root/f'reverse-{switch}-A';b=root/f'reverse-{switch}-B'
 for p in [a,b]:assert not(p/'failure.json').exists(),str(p)
 for name in ['authority.jsonl','camera.tsv']:assert(a/name).read_bytes()==(b/name).read_bytes(),(switch,name)
 assert upper(a/'pose.tsv')==upper(b/'pose.tsv'),(switch,'upper local pose/morph changed')
 ma=read(a/'motion.jsonl');mb=read(b/'motion.jsonl');assert len(ma)==len(mb)==180
 for x,y in zip(ma,mb):
  x.pop('candidate');y.pop('candidate');assert x==y,(switch,'motion/event parity')
  if x['input']:assert abs(math.hypot(*x['velocity'])-4.8)<.0001,(switch,'authority speed/collision')
  assert x['haste']==0,(switch,'haste pickup')
 rows=read(b/'locomotion.jsonl');skin=read(b/'sole.jsonl');boots=read(b/'boot-geometry.jsonl')
 anchors={};margins=[];floor=[];drift=[];flips=[];previous=None;settled=[]
 for row,sk,g in zip(rows,skin,boots):
  assert row['frame']==sk['frame']==g['frame']
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
  if previous:
   delta=[x-y for x,y in zip(d['root'],previous['root'])]
   assert abs((d['distance']-previous['distance'])-math.sqrt(sum(x*x for x in delta)))<.00001
   if sum(x*y for x,y in zip(d['travelAxis'],previous['travelAxis']))<0:
    assert sum(x*y for x,y in zip(delta,d['travelAxis']))>0,(switch,'reversed before actual render motion')
    flips.append(row['frame'])
  if d['stopAge']>0 and all(f['state']=='settled-anchor'for f in d['feet']):settled.append(d['stopAge'])
  previous=d
 assert len(flips)==1,(switch,flips)
 assert min(margins)>=.001,(switch,'reach reserve',min(margins))
 assert min(floor)>=-.001 and max(drift)<=.001,(switch,'sole ground/drift')
 assert settled and min(settled)<=.200001,(switch,'late settle',settled[:1])
 reports.append({'input_reverse_frame':switch,'actual_axis_reverse_frames':flips,'minimum_reach_margin_world':min(margins),'minimum_actual_skin_floor_height':min(floor),'max_actual_skin_anchor_drift':max(drift),'first_settle_age':min(settled)})
print(json.dumps({'scope':'15 fixed-body-heading right-to-left phase timings only; native appearance/repeated reversals not certified','cases':reports},indent=2))
