import json,pathlib,math,sys
r=pathlib.Path(sys.argv[1]);prefix=sys.argv[2] if len(sys.argv)>2 else 'headless-final'
a=r/(prefix+'-A');b=r/(prefix+'-B');report={'comparison':'A frozen foot-only Backpedal, B one additive Chest/Head curve'}
for f in ['authority.jsonl','camera.tsv','sole.jsonl']:
 assert (a/f).read_bytes()==(b/f).read_bytes(),f
 report[f]='exact'
def poses(d):
 return [[x for x in line.split('|',1)[1].split(';') if x and x.split(':',1)[0] not in {'Chest','Head'}] for line in (d/'pose.tsv').read_text().splitlines()]
assert poses(a)==poses(b),'Non Chest/Head local pose or morph changed'
report['all_other_local_pose_and_morph']='exact'
ma=[json.loads(x) for x in (a/'motion.jsonl').read_text().splitlines()];mb=[json.loads(x) for x in (b/'motion.jsonl').read_text().splitlines()]
assert len(ma)==len(mb)==180
for x,y in zip(ma,mb):
 x.pop('candidate');y.pop('candidate');assert x==y,'Attack/timing/phase/root/command mismatch'
active=ma[30:150];assert all(x['localForward']< -4.79 and abs(x['localRight'])<.001 for x in active)
wraps=sum(y['phase']<x['phase']-.1 for x,y in zip(active,active[1:]));assert wraps>=2
report['moving_wraps']=wraps;report['attack_starts']=[x['frame'] for x in ma if x['observedStart']]
report['native_postdraw_attachments']={}
for d in [a,b]:
 rows=[json.loads(x) for x in (d/'attachments.jsonl').read_text().splitlines()]
 if prefix.startswith('native'):
  assert len(rows)>=180,('missing postdraw coverage',len(rows))
  assert all(x['rows'] for x in rows),'No imported BoneAttachment evidence'
  errors=[x for row in rows for x in row['rows']]
  assert not any(x['overridePose'] for x in errors),'Unexpected attachment pose authority'
  pe=max(x['positionError'] for x in errors);be=max(x['basisError'] for x in errors)
  assert pe<1e-5 and be<1e-5,('Attachment mismatch',pe,be)
  report['native_postdraw_attachments'][d.name]={'draws':len(rows),'max_position_error':pe,'max_basis_error':be,'names':sorted({x['name'] for x in errors})}
 else:
  report['native_postdraw_attachments'][d.name]='unrun: headless has no FramePostDraw'
print(json.dumps(report,indent=2))
