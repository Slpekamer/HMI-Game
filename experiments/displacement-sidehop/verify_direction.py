"""Independent direction/boot-envelope gate; supplements actual-skin stance verification."""
import json,math,pathlib,sys
root=pathlib.Path(sys.argv[1]);reports=[]
def read(p):return [json.loads(x)for x in p.read_text().splitlines()]
for b in sorted(root.glob('*-B')):
 assert not (b/'failure.json').exists(),str(b)
 rows=read(b/'locomotion.jsonl');geom=read(b/'boot-geometry.jsonl');motion=read(b/'motion.jsonl')
 assert len(rows)==len(geom)==len(motion)
 gaps=[];flat=[];knees=[];dots=[];phase_errors=[];previous=None
 for row,g,m in zip(rows,geom,motion):
  assert row['frame']==g['frame']==m['frame']
  q=g['geometry'];gaps.append(q['lateralGapWorld']);flat.extend(f['flatBasisError']for f in q['boots']);knees.extend(f['kneePoleAlignment']for f in q['boots'])
  if m['input']!=0:assert m['localRight']>.01 and abs(m['localForward'])<.0001,('not rightward body-relative movement',b.name,row['frame'])
  d=row['diagnostics']
  if 'feet' not in d:continue
  assert d['sharedPhase'] and len(d['feet'])==2
  left,right=d['feet'];assert left['stance']==right['stance'] and abs(left['phase']-right['phase'])<.00001,('not synchronized contacts/phase',b.name,row['frame'])
  dots.append(sum(x*y for x,y in zip(d['bodyForward'],d['travelAxis'])))
  if previous is not None:
   delta=[x-y for x,y in zip(d['root'],previous['root'])];distance=math.sqrt(sum(x*x for x in delta));phase_errors.append(abs((d['distance']-previous['distance'])-distance))
   assert abs(sum(x*y for x,y in zip(delta,d['bodyForward'])))<=.00001,('root moved fore/aft',b.name,row['frame'])
  previous=d
 assert min(gaps)>0,('boots lack lateral separating plane',b.name,min(gaps))
 assert max(flat)<.0001,('foot orientation changed relative to body/rest',b.name,max(flat))
 assert min(knees)>.999,('knee pole not anatomical',b.name,min(knees))
 assert all(abs(x)<.00001 for x in dots),('travel/body axes not perpendicular',b.name)
 assert max(phase_errors)<.00001,('phase not actual displacement',b.name,max(phase_errors))
 reports.append({'case':b.name,'samples':len(rows),'min_rigid_boot_lateral_gap_world':min(gaps),'max_evaluated_flat_foot_basis_error':max(flat),'min_anatomical_knee_plane_alignment':min(knees),'max_distance_phase_accumulation_error':max(phase_errors),'travel_body_dot_range':[min(dots),max(dots)]})
assert reports,'No candidate cases'
print(json.dumps({'scope':'CPU rigid boots have a positive body-right separating gap; not trousers/GPU collision or visual acceptance','cases':reports},indent=2))
