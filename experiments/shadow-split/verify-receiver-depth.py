#!/usr/bin/env python3
"""Read actual coverage camera manifests; intersect viewport-corner rays with the existing floor."""
import json,pathlib,sys
base=pathlib.Path(sys.argv[1]);result=[]
for aspect in ['landscape','portrait']:
 for variant in ['A','B']:
  report=json.loads((base/f'{aspect}-{variant}/coverage.json').read_text())
  ratio=report['width']/report['height'];ends=[];depths=[];xz=[]
  for row in report['rows']:
   assert row['projection']==1 and row['basisZ'][1]>0
   split_end=row['Near']+(row['Far']-row['Near'])*report['light']['DirectionalShadowSplit1']
   ends.append(split_end)
   for sx in [-1,1]:
    for sy in [-1,1]:
     origin=[row['position'][i]+row['basisX'][i]*sx*row['Size']*ratio/2+row['basisY'][i]*sy*row['Size']/2 for i in range(3)]
     depth=origin[1]/row['basisZ'][1]
     point=[origin[i]-row['basisZ'][i]*depth for i in range(3)]
     assert row['Near']<depth<split_end,('visible floor exceeds first cascade',aspect,variant,row['side'],depth,split_end)
     assert abs(point[0])<40 and abs(point[2])<40,('corner not on existing 80x80 floor',point)
     depths.append(depth);xz.extend([abs(point[0]),abs(point[2])])
  result.append({'aspect':aspect,'variant':variant,'views':len(report['rows']),'minFloorDepth':min(depths),'maxFloorDepth':max(depths),'firstSplitEnd':min(ends),'maxAbsFloorXZ':max(xz),'scope':'Actual viewport-corner orthographic ray intersections with y=0 floor; all inside existing 80x80 floor and first cascade. Not proof of every offscreen caster or unseen camera pose.'})
(base/'receiver-depth.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2))
