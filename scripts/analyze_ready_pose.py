from pathlib import Path
import json,math,re,sys
root=Path(__file__).resolve().parents[1];base=Path(sys.argv[1]) if len(sys.argv)>1 else root/'evidence';lower={'Pelvis','Thigh_L','Shin_L','Foot_L','Thigh_R','Shin_R','Foot_R'};report={}
def parse(line):
 pairs={}
 for v in line.split('|',1)[1].split(';'):
  if not v:continue
  name,values=v.split(':',1);pairs[name]=[float(x) for x in re.split('[,/:]',values)]
 return pairs
for hz in (60,30,15,4):
 a=(base/f'hz{hz}-mode0/pose.tsv').read_text().splitlines();b=(base/f'hz{hz}-mode1/pose.tsv').read_text().splitlines();maxcomp=0;maxang=0;changed=0;lowerchanged=0;where=None;maxpos=0;maxscale=0;maxmorph=0
 for i,(aa,bb) in enumerate(zip(a,b)):
  x,y=parse(aa),parse(bb)
  for name in x:
   if name in lower:
    if x[name]!=y[name]:lowerchanged+=1
    continue
   err=max(abs(u-v) for u,v in zip(x[name],y[name]));
   if name.startswith('morph'):maxmorph=max(maxmorph,err)
   else:
    maxpos=max(maxpos,max(abs(u-v) for u,v in zip(x[name][4:7],y[name][4:7])))
    maxscale=max(maxscale,max([abs(u-v) for u,v in zip(x[name][7:10],y[name][7:10])]+[0]))
   if err:changed+=1
   if err>maxcomp:maxcomp=err;where=(i,name,x[name],y[name])
   if not name.startswith('morph'):
    q,r=x[name][:4],y[name][:4];dot=abs(sum(u*v for u,v in zip(q,r))/math.sqrt(sum(u*u for u in q)*sum(v*v for v in r)));maxang=max(maxang,2*math.acos(min(1,dot))*180/math.pi)
 report[str(hz)]=dict(nonlower_exactly_different_bone_frames=changed,nonlower_max_component_error=maxcomp,nonlower_max_quaternion_angle_deg=maxang,max_case=where,nonlower_max_position_component=maxpos,nonlower_max_scale_component=maxscale,max_morph_error=maxmorph,lower_different_bone_frames=lowerchanged)
(base/'pose-equivalence.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
