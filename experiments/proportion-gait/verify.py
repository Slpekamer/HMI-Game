import json,pathlib,sys,math
r=pathlib.Path(sys.argv[1]);prefix=sys.argv[2];clip=sys.argv[3]
a=r/(prefix+'-'+clip+'-A');b=r/(prefix+'-'+clip+'-B')
report={'comparison':'Original proportions vs accepted proportions, rebuilt rig/clips','clip':clip,'not_claimed':['old-bind pose/sole world parity','strict foot lock','normal combat/controller acceptance']}
for f in ['authority.jsonl','camera.tsv']:
 assert (a/f).read_bytes()==(b/f).read_bytes(),f
 report[f]='exact'
ma=[json.loads(x) for x in (a/'motion.jsonl').read_text().splitlines()];mb=[json.loads(x) for x in (b/'motion.jsonl').read_text().splitlines()]
assert len(ma)==len(mb)==180
for x,y in zip(ma,mb):
 x.pop('candidate');y.pop('candidate');assert x==y,('Attack/timing/phase/root/command mismatch',x,y)
active=ma[30:150]
for x in active:
 if clip=='Backpedal': assert x['localForward'] < -4.79 and abs(x['localRight']) < .001
 elif clip=='Run': assert x['localForward'] > 4.79 and abs(x['localRight']) < .001
 else: assert x['localRight'] > 4.79 and abs(x['localForward']) < .001
wraps=sum(y['phase']<x['phase']-.1 for x,y in zip(active,active[1:]));assert wraps>=2

def morphs(d):
 rows=[]
 for line in (d/'pose.tsv').read_text().splitlines():
  # Transform values contain '/', numeric blendshape values do not.
  rows.append([float(t.split(':',1)[1]) for t in line.split('|',1)[1].split(';') if t and '/' not in t.split(':',1)[1]])
 return rows
assert morphs(a)==morphs(b), 'Runtime facial blendshape values differ'
assert all(len(x)==3 for x in morphs(b)), 'Expected Blink/Smile/Surprise functional channels'
report['runtime_morph_values']='exact; functional target deltas verified in art artifact'
report['moving_wraps']=wraps;report['attack_starts']=[x['frame'] for x in ma if x['observedStart']]
report['sole_diagnostic']={};report['attachments']={}
for d in [a,b]:
 soles=[json.loads(x) for x in (d/'sole.jsonl').read_text().splitlines()]
 assert len(soles)==180
 feet=[f for row in soles for f in row['feet']]
 assert all(math.isfinite(v) for f in feet for p in f['points'] for v in p)
 report['sole_diagnostic'][d.name]={'minimum_y':min(f['minY'] for f in feet),'maximum_y':max(f['maxY'] for f in feet),'material_samples':sorted(set(f['count'] for f in feet))}
 report['sole_diagnostic'][d.name]['transition_windows']={}
 for label,lo,hi in [('idle',0,30),('enter_blend',30,37),('steady_motion',37,150),('exit_blend',150,157),('settled_idle',157,180)]:
  window=soles[lo:hi];fs=[f for row in window for f in row['feet']]
  steps=[math.dist(x['centroid'],y['centroid']) for prev,nxt in zip(window,window[1:]) for x,y in zip(prev['feet'],nxt['feet'])]
  report['sole_diagnostic'][d.name]['transition_windows'][label]={'min_y':min(f['minY'] for f in fs),'max_centroid_step_m':max(steps,default=0),'note':'World material travel includes Core translation; descriptive, not a strict-foot-lock gate'}
 rows=[json.loads(x) for x in (d/'attachments.jsonl').read_text().splitlines()]
 if prefix=='native':
  assert len(rows)>=180 and all(x['rows'] for x in rows)
  errors=[x for row in rows for x in row['rows']]
  assert not any(x['overridePose'] for x in errors)
  pe=max(x['positionError'] for x in errors);be=max(x['basisError'] for x in errors)
  assert pe<1e-5 and be<1e-5
  report['attachments'][d.name]={'draws':len(rows),'max_position_error':pe,'max_basis_error':be}
 else: report['attachments'][d.name]='unrun headless'
print(json.dumps(report,indent=2))
