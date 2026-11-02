#!/usr/bin/env python3
import json,pathlib,sys
base=pathlib.Path(sys.argv[1]);results=[]
for aspect in ['landscape','portrait']:
 a=json.loads((base/f'{aspect}-A/coverage.json').read_text());b=json.loads((base/f'{aspect}-B/coverage.json').read_text())
 assert len(a['rows'])==len(b['rows'])==20
 for x,y in zip(a['rows'],b['rows']):
  for r in [x,y]:assert len(set(r['hashes']))==1 and r['paused']
  assert {k:v for k,v in x.items()if k!='hashes'}=={k:v for k,v in y.items()if k!='hashes'}
 assert a['light']['DirectionalShadowSplit1']==0.1 and b['light']['DirectionalShadowSplit1']==0.015
 assert {k:v for k,v in a['light'].items()if k!='DirectionalShadowSplit1'}=={k:v for k,v in b['light'].items()if k!='DirectionalShadowSplit1'}
 assert a['light']['ShadowEnabled'] and b['light']['ShadowEnabled']
 results.append({'aspect':aspect,'views':20,'identicalCameraAndFrozenTick':True,'threeRepeatedPixelHashesStable':True,'onlyLightDifference':'DirectionalShadowSplit1','visualReview':'pending'})
(base/'verification.json').write_text(json.dumps(results,indent=2)+'\n');print(json.dumps(results))
