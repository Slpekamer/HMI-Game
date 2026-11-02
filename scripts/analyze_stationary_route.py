from pathlib import Path
import json,math,hashlib,argparse
parser=argparse.ArgumentParser(description='Compare explicitly supplied stationary-route evidence')
parser.add_argument('--baseline',required=True,type=Path)
parser.add_argument('--candidate',required=True,type=Path)
parser.add_argument('--output',required=True,type=Path)
args=parser.parse_args()
a=args.baseline.resolve();b=args.candidate.resolve()
if not a.is_dir() or not b.is_dir():parser.error('Both evidence directories must exist')
load=lambda p:[json.loads(x) for x in p.read_text().splitlines()]
A=load(a/'motion.jsonl');B=load(b/'motion.jsonl');S=load(b/'authority.jsonl')
angle=lambda x,y:abs((x-y+math.pi)%(2*math.pi)-math.pi)*180/math.pi
proof={'authorityByteEqual':(a/'authority.jsonl').read_bytes()==(b/'authority.jsonl').read_bytes(),'localBonesMorphByteEqual':(a/'pose.tsv').read_bytes()==(b/'pose.tsv').read_bytes()}
ca=[x.split('|') for x in (a/'camera.tsv').read_text().splitlines()];cb=[x.split('|') for x in (b/'camera.tsv').read_text().splitlines()]
proof['cameraByteEqual']=all(x[:3]==y[:3] for x,y in zip(ca,cb));proof['renderedPositionByteEqual']=all(x[3].split('/')[0]==y[3].split('/')[0] for x,y in zip(ca,cb));proof['motionExceptYawEqual']=all({k:v for k,v in x.items() if k!='yaw'}=={k:v for k,v in y.items() if k!='yaw'} for x,y in zip(A,B));assert all(proof.values())
proof['firstYawDifference']=next((i for i,(x,y) in enumerate(zip(A,B)) if x['yaw']!=y['yaw']),None)
intervals=[]
for f,s in enumerate(S):
 for t in s['transitions']:
  if t['Kind']!=0 or t['ProjectileKind']!=0:continue
  p=next((p for p in s['projectiles'] if p['Id']==t['Id']),None)
  if p is None or p['HitsCompleted']!=0:continue
  d=p['Direction'];desired=math.atan2(-d[0],-d[1]);end=f
  while end+1<len(B) and B[end+1]['afterActive']:end+=1
  entry={'launch':f,'end':end,'input':B[f]['input'],'frozenLaunchDirection':d,'metricsDefinition':'angular error to original launch direction, not a moving victim tracker'}
  for label,m in [('A',A),('B',B)]:
   errors=[angle(m[i]['yaw'],desired) for i in range(f,end+1)];entry[label]={'first':errors[0],'peak':max(errors),'last':errors[-1],'secondsOver45':sum(e>45 for e in errors)/60,'degreeSeconds':sum(errors)/60}
  intervals.append(entry)
proof['intervals']=intervals;proof['maxStationaryStepDegrees']=max(angle(B[i]['yaw'],B[i-1]['yaw']) for i in range(216,len(B)));assert proof['maxStationaryStepDegrees']<=4.0001
args.output.write_text(json.dumps(proof,indent=2));print(json.dumps(proof,indent=2))
