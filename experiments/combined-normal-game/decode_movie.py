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
            bits=0;samples=[]
            for i in range(10):
                rgb=im.getpixel((14+i*14,702));samples.append(rgb)
                assert max(rgb)<40 or min(rgb)>215,('ambiguous stamp',index,i,rgb)
                if sum(rgb)>3*127:bits|=1<<i
            yield index,(bits-43)&1023,im,samples
            index+=1
    finally:
        p.stdout.close()
        status=p.wait()
        if sys.exc_info()[0] is None:assert status==0,'ffmpeg decode failed'

