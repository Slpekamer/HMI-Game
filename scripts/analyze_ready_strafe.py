from pathlib import Path
import csv,json,math,hashlib,collections
root=Path(__file__).resolve().parents[1];result={}
def segment(t):return 'side-initial' if t<3 else 'stopped-first' if t<5 else 'stopped-natural-target-switch' if t<8 else 'side-later' if t<12 else 'pause' if t<13 else 'stopped-resume'
for hz in (60,30,15,4):
 a=root/f'evidence/hz{hz}-mode0';b=root/f'evidence/hz{hz}-mode1'
 if not (b/'motion.tsv').exists():continue
 proof={name:(a/name).read_bytes()==(b/name).read_bytes() for name in ('authority.tsv','camera.tsv')}
 for mode,folder in ((0,a),(1,b)):
  rows=list(csv.DictReader((folder/'motion.tsv').open(),delimiter='|'));dt=1/hz;gestures=collections.defaultdict(list)
  for r in rows:
   if r['acceptedActive']=='True' and r['gestureKind']=='Melee':gestures[r['acceptedSequence']].append(r)
  intervals=[]
  for sequence,g in gestures.items():
   errs=[float(r['gestureError'])*180/math.pi for r in g]
   intervals.append(dict(sequence=int(sequence),start=float(g[0]['time']),end=float(g[-1]['time']),segment=segment(float(g[0]['time'])),frames=len(g),first_deg=errs[0],peak_deg=max(errs),last_deg=errs[-1],over45_seconds=sum(e>45 and r['paused']=='False' for e,r in zip(errs,g))*dt,over90_frames=sum(e>90 for e in errs),entire_interval_over90=all(e>90 for e in errs)))
  segments={}
  for label in set(segment(float(r['time'])) for r in rows):
   rr=[r for r in rows if segment(float(r['time']))==label];segments[label]=dict(frames=len(rr),max_desired_error_deg=max(float(r['error']) for r in rr)*180/math.pi,diagnostics=dict(collections.Counter(r['diagnostic'] for r in rr)),targets=sorted(set(r['target'] for r in rr)))
  selected=[r for r in rows if r['right']=='True'];weighted=[r for r in rows if float(r['blend'])>0]
  invalid=lambda r:float(r['finalX'])<.60 or abs(float(r['finalZ']))>.80
  result[f'{hz}Hz-{"B" if mode else "A"}']=dict(proof=proof,frames=len(rows),segments=segments,accepted_melee_intervals=intervals,selected_right_frames=len(selected),selected_wrong_final_basis=sum(invalid(r) for r in selected),nonzero_blend_frames=len(weighted),nonzero_blend_outside_right=sum(invalid(r) for r in weighted),max_turn_deg=max(abs(float(r['turn'])) for r in rows)*180/math.pi,configured_cap_deg=240/hz)
(root/'evidence/ready-metrics.json').write_text(json.dumps(result,indent=2))
for k,v in result.items():
 print(k,v['proof'],'right',v['selected_right_frames'],'blendoutside',v['nonzero_blend_outside_right'],'intervals',v['accepted_melee_intervals'])
