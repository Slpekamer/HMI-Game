"""Compare frozen A/B drill data. Never equate these checks with visual acceptance."""
import json, math, pathlib, sys
root=pathlib.Path(sys.argv[1]); prefix=sys.argv[2] if len(sys.argv)>2 else 'headless-final'
lower={'Pelvis','Thigh_L','Shin_L','Foot_L','Thigh_R','Shin_R','Foot_R'}
report={}
for clip in ['StrafeR','Backpedal']:
 a=root/(prefix+'-'+clip+'-A'); b=root/(prefix+'-'+clip+'-B')
 for name in ['authority.jsonl','camera.tsv']:
  assert (a/name).read_bytes()==(b/name).read_bytes(),(clip,name,'A/B mismatch')
 poses=[]
 for d in [a,b]:
  rows=[]
  for line in (d/'pose.tsv').read_text().splitlines():
   frame,p=line.split('|',1)
   rows.append([x for x in p.split(';') if x and x.split(':',1)[0] not in lower])
  poses.append(rows)
 assert poses[0]==poses[1],(clip,'upper local/morph mismatch')
 states=[]
 for d in [a,b]:
  m=[json.loads(x) for x in (d/'motion.jsonl').read_text().splitlines()]
  assert len(m)==180,(clip,'incomplete route',len(m))
  active=m[30:150]
  speeds=[math.hypot(x['localRight'],x['localForward']) for x in active]
  assert min(speeds)>4.79 and max(speeds)<4.81,(clip,'clamped speed',min(speeds),max(speeds))
  assert all((x['localRight']>4.79 and abs(x['localForward'])<.001) if clip=='StrafeR' else (x['localForward']< -4.79 and abs(x['localRight'])<.001) for x in active),(clip,'wrong sector')
  wraps=sum(y['phase']<x['phase']-.1 for x,y in zip(active,active[1:]))
  assert wraps>=2,(clip,'insufficient cycles',wraps)
  sole=[json.loads(x) for x in (d/'sole.jsonl').read_text().splitlines()]
  assert all(math.isfinite(c) for x in sole for foot in x['feet'] for p in foot['points'] for c in p)
  # Per-source-point displacement on declared candidate stance intervals, after entrance blend.
  drift=[]
  if d==b:
   for side,offset in [('Foot_R',.6),('Foot_L',.1)]:
    groups=[];group=[]
    for row in sole[40:150]:
     phase=((row['phase']-1/30)/.8+offset)%1
     if row['phase']>=1/30 and phase<=.6:
      if group and row['phase']<group[-1]['phase']:groups.append(group);group=[]
      group.append(row)
     elif group:groups.append(group);group=[]
    if group:groups.append(group)
    for group in groups:
     if len(group)<3:continue
     first=next(f for f in group[0]['feet'] if f['side']==side);last=next(f for f in group[-1]['feet'] if f['side']==side)
     ds=[math.hypot(q[0]-p[0],q[2]-p[2]) for p,q in zip(first['points'],last['points'])]
     drift.append({'side':side,'frames':[group[0]['frame'],group[-1]['frame']],'seconds':(len(group)-1)/60,'mean_material_point_drift_m':sum(ds)/len(ds),'max_material_point_drift_m':max(ds)})
  states.append({'mode':d.name.rsplit('-',1)[1],'frames':len(m),'moving_speed_range': [min(speeds),max(speeds)],'moving_wraps':wraps,'observed_attack_starts':[x['frame'] for x in m if x['observedStart']],'sole_min_y':min(f['minY'] for x in sole for f in x['feet']),'sole_max_y':max(f['maxY'] for x in sole for f in x['feet']),'candidate_stance_segments':drift})
 report[clip]={'authority_exact':True,'camera_actor_transform_exact':True,'upper_local_morph_exact':True,'states':states}
print(json.dumps(report,indent=2))
