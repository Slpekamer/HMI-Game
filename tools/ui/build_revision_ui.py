from pathlib import Path
p=Path(__file__).resolve().parents[2]/'content/ui/revision';p.mkdir(exist_ok=True,parents=True)
def save(name,w,h,s): (p/(name+'.svg')).write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">'+s+'</svg>')
def star(x,y,r,c):
 import math
 pts=' '.join(f'{x+__import__("math").sin(i*math.pi/5)*r*(1 if i%2==0 else .48):.1f},{y-__import__("math").cos(i*math.pi/5)*r*(1 if i%2==0 else .48):.1f}' for i in range(10))
 return f'<polygon points="{pts}" fill="{c}" stroke="#fff1ce" stroke-width="2"/>'
colors={'purple':('#d7b7ef','#9867bf'),'blue':('#a6e0ff','#5ba5d5'),'coral':('#ffb4a8','#ed847d'),'gold':('#ffe8a5','#e6b95d'),'cream':('#fffaf0','#f0d0ab')}
for name,(light,dark) in colors.items():
 s=f'<defs><linearGradient id="rim" x2="0" y2="1"><stop stop-color="{light}"/><stop offset="1" stop-color="{dark}"/></linearGradient><linearGradient id="paper" x2=".3" y2="1"><stop stop-color="#fffdf7"/><stop offset="1" stop-color="#ffecd8"/></linearGradient></defs><rect x="5" y="10" width="246" height="242" rx="30" fill="{dark}" opacity=".6"/><rect x="3" y="3" width="250" height="244" rx="30" fill="url(#rim)" stroke="#fff2d6" stroke-width="5"/><rect x="13" y="14" width="230" height="220" rx="23" fill="url(#paper)" stroke="{light}" stroke-width="3"/><path d="M29 21H222" stroke="white" stroke-width="3" opacity=".85"/>'
 for x,y in [(26,31),(230,31),(26,223),(230,223)]:s+=star(x,y,7,light)
 save('frame_'+name,256,256,s)
# A purpose-built, text-free candy village backdrop, not an approved concept cropped into the UI.
s='<defs><linearGradient id="sky" x2="0" y2="1"><stop stop-color="#a6c9f4"/><stop offset=".55" stop-color="#fbd2cb"/><stop offset="1" stop-color="#f8d9ab"/></linearGradient><linearGradient id="land" x2="0" y2="1"><stop stop-color="#ffe3be"/><stop offset="1" stop-color="#eeb5b9"/></linearGradient></defs><rect width="1280" height="720" fill="url(#sky)"/>'
for x,y,r in [(80,90,65),(320,35,55),(1080,70,80),(1210,155,65),(690,80,30)]:
 for dx,dy,rr in [(0,0,r),(-r*.65,15,r*.6),(r*.7,18,r*.7)]:s+=f'<circle cx="{x+dx}" cy="{y+dy}" r="{rr}" fill="#fff4e9" opacity=".72"/>'
s+='<path d="M0 400 Q250 240 500 370T1280 350V720H0Z" fill="#d9bbdc"/><path d="M0 480 Q280 300 590 450T1280 410V720H0Z" fill="url(#land)"/>'
for i,(x,y,w,h,c) in enumerate([(0,190,125,350,'#e8a5c2'),(130,160,95,320,'#b4a6dc'),(1040,180,130,350,'#a2cfeb'),(1180,130,120,380,'#e8b1d8'),(245,260,75,210,'#f3c98d'),(935,250,85,210,'#e5b0c5')]):
 s+=f'<rect x="{x+7}" y="{y+10}" width="{w}" height="{h}" rx="25" fill="#996a99" opacity=".18"/><rect x="{x}" y="{y}" width="{w}" height="{h}" rx="25" fill="{c}" stroke="#fff0d6" stroke-width="6"/>'
 for yy in range(y+40,y+h,52):s+=f'<path d="M{x+8} {yy}H{x+w-8}" stroke="#fff2df" stroke-width="3" opacity=".45"/>'
 s+=f'<ellipse cx="{x+w/2}" cy="{y}" rx="{w*.55}" ry="25" fill="#ffe7bc" stroke="#fff5dc" stroke-width="5"/>'+star(x+w/2,y-35,28,'#ffdd79')
for x,y,r in [(55,360,45),(1195,320,58),(215,145,37),(1050,120,38)]:
 s+=f'<path d="M{x} {y}v200" stroke="#fff4dd" stroke-width="15"/><circle cx="{x}" cy="{y}" r="{r}" fill="#ffabc5" stroke="#fff0d4" stroke-width="6"/><path d="M{x-r*.5} {y}q{r*.5} {-r*.8} {r} 0q{-r*.5} {r*.8} {-r} 0" fill="none" stroke="#ffe1dd" stroke-width="12"/>'
for i in range(16):
 x=i*90-25;y=660+(i%3)*12;c=['#e9b8d3','#c1b4e5','#afd8ed','#f3d19a'][i%4]
 s+=f'<rect x="{x}" y="{y}" width="100" height="80" rx="18" fill="{c}" stroke="#fff0d8" stroke-width="5"/>'+star(x+40,y+25,13,'#ffdf92')
s+='<rect width="1280" height="720" fill="#fff1df" opacity=".12"/>'
save('candy_village',1280,720,s)
for name,top,bottom,edge in [('primary','#ff9a93','#ff6577','#e84e68'),('secondary','#ceb0ec','#a57bd4','#8b5fb9'),('disabled','#c5b4c5','#a492a6','#8b7a91')]:
 s=f'<defs><linearGradient id="b" x2="0" y2="1"><stop stop-color="{top}"/><stop offset="1" stop-color="{bottom}"/></linearGradient></defs><rect x="3" y="10" width="314" height="67" rx="29" fill="{edge}"/><rect x="3" y="3" width="314" height="66" rx="29" fill="url(#b)" stroke="#fff0dd" stroke-width="4"/><path d="M30 16H290" stroke="#fff2e6" stroke-width="3" opacity=".6" stroke-linecap="round"/>'
 save('button_'+name,320,80,s)
