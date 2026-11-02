#!/usr/bin/env python3
"""Decode embedded stamps; align A/B by simulation tick, never guessed AVI index.
Requires ffmpeg/ffprobe, Python Pillow. Keeps every requested simulation tick (default 0..179).
No resizing, cropping, retiming or interpolation. Outputs two 1280x720 streams
side by side; labels sit in additional top margin. Internal review only.
"""
import argparse, json, pathlib, subprocess, tempfile, sys
from PIL import Image, ImageDraw


def frames(path):
    info=json.loads(subprocess.check_output(['ffprobe','-v','error','-select_streams','v:0','-show_entries','stream=width,height,r_frame_rate','-of','json',str(path)]))['streams'][0]
    assert (info['width'],info['height'],info['r_frame_rate'])==(1280,720,'60/1'),info
    p=subprocess.Popen(['ffmpeg','-v','error','-i',str(path),'-f','rawvideo','-pix_fmt','rgb24','-'],stdout=subprocess.PIPE)
    index=0
    try:
        while True:
            data=p.stdout.read(1280*720*3)
            if not data:break
            assert len(data)==1280*720*3,'truncated frame'
            im=Image.frombytes('RGB',(1280,720),data)
            bits=0
            for i in range(8):
                rgb=im.getpixel((14+i*14,702))
                assert max(rgb)<40 or min(rgb)>215,('ambiguous stamp',index,i,rgb)
                if sum(rgb)>3*127:bits|=1<<i
            yield index,(bits-43)&255,im
            index+=1
    finally:
        p.stdout.close()
        status=p.wait()
        if sys.exc_info()[0] is None:assert status==0,'ffmpeg decode failed'


def main():
    ap=argparse.ArgumentParser();ap.add_argument('base',type=pathlib.Path);ap.add_argument('scenario',choices=['flight','stance','restart','restart4','restart1']);ap.add_argument('output',type=pathlib.Path);ap.add_argument('--frames',type=int,default=180);a=ap.parse_args();assert 1<=a.frames<=256
    a.output.mkdir(parents=True,exist_ok=False)
    mapping={}
    with tempfile.TemporaryDirectory() as temp:
        temp=pathlib.Path(temp)
        for variant in ['A','B']:
            seen={}; rows=[];previous=-1
            for index,tick,im in frames(a.base/f'native-{a.scenario}-{variant}'/'capture.avi'):
                assert 0<=tick<a.frames and tick>=previous,('bad stamp order',variant,index,tick)
                duplicate=tick in seen
                rows.append({'encoded_frame':index,'simulation_tick':tick,'duplicate':duplicate})
                if not duplicate:
                    seen[tick]=index;im.save(temp/f'{variant}-{tick:03}.png')
                previous=tick
            assert sorted(seen)==list(range(a.frames)),('missing ticks',variant)
            mapping[variant]={'source':f'native-{a.scenario}-{variant}/capture.avi','frames':rows,'output_tick_to_encoded_frame':[seen[i] for i in range(a.frames)]}
        for tick in range(a.frames):
            canvas=Image.new('RGB',(2560,752),'#181818');d=ImageDraw.Draw(canvas)
            d.text((12,10),'A: rejected in-place Run | fixed camera | original pixels',fill='white')
            d.text((1292,10),f'B: displacement/contact Run WIP | case: {a.scenario} | internal visual gate',fill='white')
            for i,v in enumerate(['A','B']):
                with Image.open(temp/f'{v}-{tick:03}.png') as im:canvas.paste(im,(1280*i,32))
            canvas.save(temp/f'pair-{tick:03}.png')
        subprocess.run(['ffmpeg','-v','error','-framerate','60','-i',str(temp/'pair-%03d.png'),'-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(a.output/'comparison.mp4')],check=True)
    (a.output/'frame-map.json').write_text(json.dumps({'fps':60,'output_frames':a.frames,'mapping':mapping,'rule':'First encoded occurrence of each decoded simulation tick; duplicate stamps explicitly recorded. Complete cycle, no retiming.'},indent=2)+'\n')

if __name__=='__main__':main()
