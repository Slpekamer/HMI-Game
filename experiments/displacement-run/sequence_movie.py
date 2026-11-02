#!/usr/bin/env python3
"""Full-width A then B, Chinese labels, all unique simulation ticks at original 60 fps."""
import argparse,json,pathlib,subprocess,tempfile
from PIL import Image,ImageDraw,ImageFont
from align_movie import frames
p=argparse.ArgumentParser();p.add_argument('base',type=pathlib.Path);p.add_argument('scenario');p.add_argument('output',type=pathlib.Path);p.add_argument('--frames',type=int,required=True);p.add_argument('--title',required=True);p.add_argument('--font',type=pathlib.Path,required=True);a=p.parse_args()
assert 1<=a.frames<=256 and a.font.is_file();font=ImageFont.truetype(str(a.font),20);a.output.mkdir(parents=True,exist_ok=False);mapping={}
with tempfile.TemporaryDirectory() as tmp:
 tmp=pathlib.Path(tmp)
 for section,variant in enumerate(['A','B']):
  seen={};rows=[];previous=-1
  for index,tick,im,samples in frames(a.base/f'native-{a.scenario}-{variant}/capture.avi'):
   assert 0<=tick<a.frames and tick>=previous,('stamp order',variant,index,tick)
   duplicate=tick in seen;rows.append({'encoded_frame':index,'simulation_tick':tick,'duplicate':duplicate,'stamp_center_rgb':samples})
   if not duplicate:
    seen[tick]=index;out=Image.new('RGB',(1280,752),'#181818');label='A 原地动画（已否决）' if variant=='A' else 'B 位移驱动实验（待审核）';ImageDraw.Draw(out).text((12,3),a.title+'  |  '+label+'  |  原尺度 / 正常速度',font=font,fill='white');out.paste(im,(0,32));out.save(tmp/f'{section*a.frames+tick:04}.png',compress_level=1)
   previous=tick
  assert sorted(seen)==list(range(a.frames)),('missing simulation ticks',variant)
  mapping[variant]={'output_start_frame':section*a.frames,'source':f'native-{a.scenario}-{variant}/capture.avi','frames':rows,'simulation_tick_to_encoded_frame':[seen[i]for i in range(a.frames)]}
 subprocess.run(['ffmpeg','-v','error','-framerate','60','-i',str(tmp/'%04d.png'),'-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(a.output/'comparison-sequential.mp4')],check=True)
(a.output/'frame-map.json').write_text(json.dumps({'layout':'A then B, full 1280x720 source plus32px header','fps':60,'output_frames':2*a.frames,'stamp_thresholds':'all RGB<40 black; all RGB>215 white; ambiguous rejected','rule':'Keep every unique simulation tick. Repeated terminal encoded frames are omitted explicitly; no interpolation, crop or rescale.','mapping':mapping},indent=2)+'\n')
