#!/usr/bin/env python3
"""Actual independent melee and launch event observation; not synthetic event coverage."""
import json,pathlib,sys
root=pathlib.Path(sys.argv[1]);a=root/sys.argv[2];b=root/sys.argv[3]
assert (a/'authority.jsonl').read_bytes()==(b/'authority.jsonl').read_bytes()
sa=json.loads((a/'summary.json').read_text());sb=json.loads((b/'summary.json').read_text())
assert sa['frame']==sb['frame'] and sa['melee']==sb['melee']>0 and sa['launches']==sb['launches']>0
for p in [a,b]:
 log=(p/'runtime.log').read_text();assert 'LAUNCH_POSE_MIXED_PASS'in log and not any(t in log for t in ['ERROR:','FATAL','Unhandled'])
rows=[json.loads(x)for x in(b/'cues.jsonl').read_text().splitlines()];assert len(rows)==sb['frame'] and [x['frame']for x in rows]==list(range(sb['frame']))
melee=[];interrupted=[];launches=[]
for i,r in enumerate(rows):
 if r['playerMelee'] and not r['playerLaunch']:
  assert r['sample'] is None,(i,'melee retained launch sample');melee.append(i)
  if r['pose']['state']=='cancel-to-original-gesture':
   interrupted.append(i)
   # No later event in this narrow window: old cue must hand back by the fifth frame.
   window=rows[i+1:i+6]
   if len(window)==5 and not any(q['playerLaunch']or q['playerMelee']for q in window):
    assert window[-1]['pose']['state']=='idle',(i,'cancellation did not settle')
 if r['playerLaunch']:
  assert r['sample'] and r['sample']['Tick'] in r['playerLaunch'],(i,'unmatched launch')
  assert r['pose']['state']=='launch-key-wip' and r['pose']['Tick']==r['sample']['Tick']
  launches.append(i)
assert interrupted and launches
print(json.dumps({'frames':len(rows),'meleeFrames':melee,'realLaunchFrames':launches,'interruptedCueFrames':interrupted,'authorityByteExact':True,'scope':'Actual natural independent melee and projectile events, including cue cancellation. Does not cover same-tick coincidence, catch-up ordering or skin geometry.'},indent=2))
