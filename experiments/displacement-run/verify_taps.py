"""Bounded ordinary-tap checks against separate CPU-skinned sole measurements."""
import json,math,pathlib,sys
r=pathlib.Path(sys.argv[1]);reports=[]
def read(p):return [json.loads(x) for x in p.read_text().splitlines()]
lower={'Pelvis','Thigh_L','Shin_L','Foot_L','Thigh_R','Shin_R','Foot_R'}
def upper(p):return [[x for x in line.split('|',1)[1].split(';') if x and x.split(':',1)[0] not in lower] for line in p.read_text().splitlines()]
for start in [30,150]:
 for pattern in ['010','0110','0101010101010101','0011001100110011']:
  for end in [0,1]:
   name=f'start-{start}-pattern-{pattern}-end-{end}';a=r/(name+'-A');b=r/(name+'-B')
   for p in [a,b]:assert not (p/'failure.json').exists(),str(p)
   for file in ['authority.jsonl','camera.tsv']:assert (a/file).read_bytes()==(b/file).read_bytes(),(name,file)
   assert upper(a/'pose.tsv')==upper(b/'pose.tsv'),name
   ma=read(a/'motion.jsonl');mb=read(b/'motion.jsonl');assert len(ma)==len(mb)==195
   for x,y in zip(ma,mb):x.pop('candidate');y.pop('candidate');assert x==y,(name,'event/movement')
   rows=read(b/'locomotion.jsonl');skin=read(b/'sole.jsonl');assert len(rows)==len(skin)==195
   margins=[];floor=[];drift=[];anchors={};boundary=[]
   for index,(row,sk) in enumerate(zip(rows,skin)):
    d=row['diagnostics'];assert row['frame']==sk['frame']==index
    if 'feet' not in d:continue
    assert not d['failure'],d
    for i,f in enumerate(d['feet']):
     points=sk['feet'][i]['points'];margins.append(f['reachMargin']);floor.extend(p[1]-d['groundY'] for p in points)
     if f['stance']:
      orig=anchors.setdefault((i,tuple(f['anchor'])),points);drift.extend(math.dist(x,y) for x,y in zip(orig,points))
     if start<=index<=start+len(pattern)+5:
      previous=skin[index-1]['feet'][i]['points'];boundary.append({'frame':index,'side':f['side'],'state':f['state'],'skin_step_world':max(math.dist(x,y) for x,y in zip(points,previous)),'body_drop_world':d['bodyDrop']})
   assert min(margins)>=.001,(name,'reach',min(margins));assert min(floor)>=-.001,(name,'penetration',min(floor));assert max(drift)<=.001,(name,'slip',max(drift))
   final=rows[-1]['diagnostics']
   if end:assert final['state']=='running',name
   else:assert all(f['state']=='settled-anchor' for f in final['feet']),(name,'not settled')
   reports.append({'case':name,'minimum_reach_margin_world':min(margins),'minimum_skin_sole_above_floor_world':min(floor),'max_actual_skin_stance_drift_world':max(drift),'boundary':boundary,'authority_camera_upper_morph_event_parity':'exact'})
print(json.dumps({'scope':'16 bounded tap sequences, not arbitrary heading or variable-framerate certification','continuity':'Actual boundary samples reported, normal-speed visual review required','cases':reports},indent=2))
