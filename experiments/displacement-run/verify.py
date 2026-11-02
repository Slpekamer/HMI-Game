import json,pathlib,math,sys
r=pathlib.Path(sys.argv[1]);prefix=sys.argv[2];scenario=sys.argv[3];a=r/f'{prefix}-{scenario}-A';b=r/f'{prefix}-{scenario}-B'
report={'scope':'One fixed-heading forward Run contact/flight prototype, not normal controller','scenario':scenario}
for name in ['authority.jsonl','camera.tsv']:
 assert (a/name).read_bytes()==(b/name).read_bytes(),name;report[name]='byte-identical'
def read(p):return [json.loads(x) for x in p.read_text().splitlines()]
ma=read(a/'motion.jsonl');mb=read(b/'motion.jsonl');assert len(ma)==len(mb)==180
for x,y in zip(ma,mb):x.pop('candidate');y.pop('candidate');assert x==y,'Phase/event/speed mismatch'
report['attack_starts']=[x['frame'] for x in ma if x['observedStart']]
lower={'Pelvis','Thigh_L','Shin_L','Foot_L','Thigh_R','Shin_R','Foot_R'}
def upper(p):return [[x for x in line.split('|',1)[1].split(';') if x and x.split(':',1)[0] not in lower] for line in p.read_text().splitlines()]
assert upper(a/'pose.tsv')==upper(b/'pose.tsv'),'Upper Attack/local/morph changed'
report['nonlower_local_pose_and_runtime_morph']='exact'
rows=read(b/'locomotion.jsonl');skin=read(b/'sole.jsonl');old=read(a/'sole.jsonl');anchors={};margins=[];drift=[];old_drift=[];solemin=[];falls=[];previousfall=-1;end=[]
axis=ma[60]['velocity'];mag=math.hypot(*axis);axis=[x/mag for x in axis]
for row,sk,skold in zip(rows,skin,old):
 d=row['diagnostics']
 if not d or 'feet' not in d:continue
 assert d['failure']=='',d
 for i,f in enumerate(d['feet']):
  margins.append(f['reachMargin']);solemin.extend(p[1]-d['groundY'] for p in sk['feet'][i]['points'])
  if f['stance']:
   key=(f['side'],tuple(f['anchor']))
   if key not in anchors:anchors[key]=(sk['feet'][i]['points'],skold['feet'][i]['points'])
   orig,oldorig=anchors[key]
   drift.append(max(math.hypot(x[0]-y[0],x[2]-y[2]) for x,y in zip(sk['feet'][i]['points'],orig)))
   old_drift.append(max(math.hypot(x[0]-y[0],x[2]-y[2]) for x,y in zip(skold['feet'][i]['points'],oldorig)))
 if d['footfalls']!=previousfall and d['footfalls']>0:
  fs=[f for f in d['feet'] if f['state']=='stance-anchor'];assert len(fs)==1
  f=fs[0];falls.append({'frame':row['frame'],'ordinal':d['footfalls'],'side':f['side'],'anchor':f['anchor']})
 previousfall=d['footfalls']
 if d['stopAge']>0 and all(f['state']=='settled-anchor' for f in d['feet']):end.append((row['frame'],d['stopAge']))
assert min(margins)>=.001,'Reach reserve below1mm world'
assert max(drift)<=.001,'Stance sole drift'
assert min(solemin)>=-.001,'Penetration'
assert end and end[0][1]<=.200001,'Late settle'
spacings=[(y['anchor'][0]-x['anchor'][0])*axis[0]+(y['anchor'][2]-x['anchor'][2])*axis[1] for x,y in zip(falls,falls[1:])]
assert all(abs(s-1.2)<.0001 for s in spacings),spacings
intervals=[y['frame']-x['frame'] for x,y in zip(falls,falls[1:])];assert all(x==15 for x in intervals),intervals
report.update({'minimum_reach_margin_world':min(margins),'max_actual_skin_stance_drift_world':max(drift),'baseline_actual_skin_motion_over_same_stance_windows_world':max(old_drift),'minimum_actual_skin_sole_relative_floor':min(solemin),'first_settled_frame_and_stop_age':end[0],'fall_events':falls,'forward_fall_spacing_world':spacings,'fall_intervals_ticks':intervals,'nominal_contact_duration':'0.24 actual distance /4.8 =0.05sec; three tick intervals/four endpoints, not three sample spans','sole_sample_limit':'rigid material samples include duplicate vertices; not independent contacts or GPU certification'})
report['postdraw_attachments']='unrun headless'
if prefix=='native':
 report['postdraw_attachments']={}
 for folder in [a,b]:
  att=read(folder/'attachments.jsonl');assert len(att)>=180 and all(x['rows'] for x in att)
  e=[x for row in att for x in row['rows']];pe=max(x['positionError'] for x in e);be=max(x['basisError'] for x in e);assert pe<1e-5 and be<1e-5
  report['postdraw_attachments'][folder.name]={'samples':len(att),'position_error':pe,'basis_error':be}
print(json.dumps(report,indent=2))
