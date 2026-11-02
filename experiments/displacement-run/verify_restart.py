"""Independent CPU-skinned sole checks; boundary motion is reported, not visual certification."""
import json, math, pathlib, sys
root=pathlib.Path(sys.argv[1]); reports=[];lower={'Pelvis','Thigh_L','Shin_L','Foot_L','Thigh_R','Shin_R','Foot_R'}
def read(p):return [json.loads(x) for x in p.read_text().splitlines()]
def upper(p):return [[x for x in line.split('|',1)[1].split(';') if x and x.split(':',1)[0] not in lower] for line in p.read_text().splitlines()]
for stance in [0,1]:
 for delay in range(14):
  a=root/f'stop-{stance}-delay-{delay}-A';b=root/f'stop-{stance}-delay-{delay}-B'
  for path in [a,b]:assert not (path/'failure.json').exists(),str(path)
  for file in ['authority.jsonl','camera.tsv']:assert (a/file).read_bytes()==(b/file).read_bytes(),(b.name,file)
  assert upper(a/'pose.tsv')==upper(b/'pose.tsv'),'Upper/morph local mismatch'
  ma=read(a/'motion.jsonl');mb=read(b/'motion.jsonl')
  assert len(ma)==len(mb)==240
  for x,y in zip(ma,mb):x.pop('candidate');y.pop('candidate');assert x==y,'Motion/event mismatch'
  rows=read(b/'locomotion.jsonl');skin=read(b/'sole.jsonl');assert len(rows)==len(skin)==240
  anchors={};drift=[];margins=[];floor=[];boundary=[];resume=(153 if stance else 150)+delay
  for index,(row,sk) in enumerate(zip(rows,skin)):
   d=row['diagnostics'];assert row['frame']==sk['frame']==index
   if not d or 'feet' not in d:continue
   assert not d['failure'],d
   for i,f in enumerate(d['feet']):
    pts=sk['feet'][i]['points'];margins.append(f['reachMargin']);floor.extend(p[1]-d['groundY'] for p in pts)
    if f['stance']:
     key=(f['side'],tuple(f['anchor']));orig=anchors.setdefault(key,pts)
     drift.append(max(math.dist(x,y) for x,y in zip(pts,orig)))
    if resume-1<=index<=resume+10:
     prev=skin[index-1]['feet'][i]['points']
     boundary.append({'frame':index,'side':f['side'],'state':f['state'],'max_skin_sole_step_world':max(math.dist(x,y) for x,y in zip(pts,prev)),'reach_margin_world':f['reachMargin']})
  assert min(margins)>=.001,(b.name,'reach',min(margins))
  assert min(floor)>=-.001,(b.name,'floor',min(floor))
  assert max(drift)<=.001,(b.name,'stance drift',max(drift))
  assert rows[-1]['diagnostics']['state']=='running',(b.name,'did not return to running')
  reports.append({'stance_stop':bool(stance),'restart_delay_ticks':delay,'minimum_reach_margin_world':min(margins),'minimum_skin_sole_above_floor_world':min(floor),'max_actual_skin_stance_drift_world':max(drift),'boundary':boundary,'authority_camera_upper_morph_event_parity':'exact'})
print(json.dumps({'scope':'28 bounded restart timings; independent CPU skin samples, not GPU/native appearance certification','continuity':'Boundary displacements reported for review; do not equate no exception with visually continuous','cases':reports},indent=2))
