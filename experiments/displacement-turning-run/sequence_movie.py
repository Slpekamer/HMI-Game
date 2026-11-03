#!/usr/bin/env python3
"""Candidate-only full-view turning samples, original 60fps ticks, no crop/speed change."""
import argparse,json,pathlib,subprocess,tempfile,sys
from PIL import Image,ImageDraw,ImageFont
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]/'displacement-run'))
from align_movie import frames
p=argparse.ArgumentParser();p.add_argument('base',type=pathlib.Path);p.add_argument('output',type=pathlib.Path);p.add_argument('--font',type=pathlib.Path,required=True);a=p.parse_args()
font=ImageFont.truetype(str(a.font),20);mapping={};cases=[('turn-90','90° 转向'),('turn--90','−90° 转向'),('turn-180','180° 反向转身')]
with tempfile.TemporaryDirectory()as tmp:
 tmp=pathlib.Path(tmp)
 for section,(case,label)in enumerate(cases):
  seen={};rows=[];previous=-1
  for index,tick,im,samples in frames(a.base/f'native-{case}-B/capture.avi'):
   assert 0<=tick<180 and tick>=previous,('stamp order',case,index,tick)
   duplicate=tick in seen;rows.append({'encoded_frame':index,'simulation_tick':tick,'duplicate':duplicate,'stamp_center_rgb':samples})
   if not duplicate:
    seen[tick]=index;out=Image.new('RGB',(1280,752),'#181818');ImageDraw.Draw(out).text((12,3),'身体随移动转向 + 位移跑步实验  |  '+label+'  |  原尺度 / 正常速度',font=font,fill='white');out.paste(im,(0,32));out.save(tmp/f'{section*180+tick:04}.png',compress_level=1)
   previous=tick
  assert sorted(seen)==list(range(180)),('missing ticks',case)
  mapping[case]={'output_start_frame':section*180,'source':f'native-{case}-B/capture.avi','frames':rows,'simulation_tick_to_encoded_frame':[seen[i]for i in range(180)]}
 subprocess.run(['ffmpeg','-v','error','-framerate','60','-i',str(tmp/'%04d.png'),'-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(a.output)],check=True)
(a.base/'frame-map.json').write_text(json.dumps({'fps':60,'frames':540,'rule':'Every unique simulation tick retained at original speed/full view. Only repeated terminal capture frames omitted.','mapping':mapping},indent=2)+'\n')
