"""Original vector UI generator recovered from active conversation, 2026-10-31."""
from pathlib import Path
import json,hashlib,os
out=Path(os.environ.get('HMI_ART_OUTPUT',Path(__file__).resolve().parent.parent/'v01'))/'ui';out.mkdir(exist_ok=True,parents=True)
start='<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128">'
def save(name,body):(out/(name+'.svg')).write_text(start+body+'</svg>')
def badge(color,body):return f'<circle cx="64" cy="68" r="54" fill="#48284f" opacity=".12"/><circle cx="64" cy="62" r="53" fill="{color}" stroke="#fff2d9" stroke-width="5"/>'+body
shapes={
'orbit':'<ellipse cx="64" cy="65" rx="44" ry="18" transform="rotate(-28 64 65)" fill="none" stroke="#fff2d9" stroke-width="7"/><circle cx="65" cy="61" r="24" fill="#8e5cce" stroke="#fff2d9" stroke-width="4"/><path d="m65 42 5 11 12 2-9 8 2 12-10-6-11 6 3-12-9-8 12-2z" fill="#ffdb39"/><circle cx="29" cy="84" r="8" fill="#ffdb39"/>',
'crit':'<path d="M22 88 51 60 76 75 104 37" fill="none" stroke="#fff2d9" stroke-width="10" stroke-linecap="round" stroke-linejoin="round"/><path d="m86 36 20-3-1 21" fill="none" stroke="#fff2d9" stroke-width="8" stroke-linecap="round"/><circle cx="45" cy="57" r="15" fill="#ffdb39" stroke="#c85a32" stroke-width="4"/>',
'honey':'<g fill="#ffdb39" stroke="#b66c18" stroke-width="4"><path d="m64 25 19 11v22L64 69 45 58V36z"/><path d="m43 62 19 11v22L43 106 24 95V73z"/><path d="m85 62 19 11v22l-19 11-19-11V73z"/></g>',
'summon':'<g fill="#fff2d9"><ellipse cx="64" cy="81" rx="27" ry="22"/><ellipse cx="29" cy="55" rx="10" ry="14" transform="rotate(-25 29 55)"/><ellipse cx="49" cy="37" rx="10" ry="14"/><ellipse cx="77" cy="37" rx="10" ry="14"/><ellipse cx="99" cy="55" rx="10" ry="14" transform="rotate(25 99 55)"/></g>',
'bounce':'<path d="m64 21 10 22 24-10-7 25 24 8-25 11 7 25-25-9-9 24-11-24-25 8 9-25-24-10 24-9-7-25 23 10z" fill="#fff2d9"/><path d="m64 45 6 13 15 2-11 10 3 15-13-7-13 7 3-15-11-10 15-2z" fill="#ed628d"/>',
'coin':'<circle cx="64" cy="62" r="39" fill="#ffdb39" stroke="#b87520" stroke-width="5"/><circle cx="64" cy="62" r="30" fill="none" stroke="#fff2d9" stroke-width="3"/><path d="M46 73c0-16 36-16 36 0s-36 16-36 0" fill="#d99b22"/><g fill="#d99b22"><circle cx="43" cy="52" r="6"/><circle cx="57" cy="43" r="6"/><circle cx="73" cy="43" r="6"/><circle cx="86" cy="53" r="6"/></g>',
'heart':'<path d="M64 100 26 65C3 38 44 16 64 44c20-28 61-6 38 21Z" fill="#f36c92" stroke="#fff2d9" stroke-width="5"/><path d="M32 48q7-13 19-4" fill="none" stroke="#ffd8e3" stroke-width="6" stroke-linecap="round"/>',
'speed':'<path d="M75 24 34 68h24l-7 39 44-53H70z" fill="#ffdb39" stroke="#fff2d9" stroke-width="4"/>',
'shield':'<path d="M64 23 99 38v26c0 21-19 34-35 43-16-9-35-22-35-43V38Z" fill="#a885d6" stroke="#fff2d9" stroke-width="6"/><path d="m46 65 12 12 25-29" fill="none" stroke="#fff2d9" stroke-width="8" stroke-linecap="round" stroke-linejoin="round"/>',
'soda':'<rect x="39" y="22" width="50" height="85" rx="13" fill="#6aa8e8" stroke="#fff2d9" stroke-width="4"/><ellipse cx="64" cy="25" rx="24" ry="7" fill="#dce4f2"/><ellipse cx="64" cy="26" rx="10" ry="3" fill="#8290aa"/><path d="m70 42-20 26h14l-5 22 23-30H67z" fill="#fff2d9"/>',
'insoles':'<g fill="#ffdb39" stroke="#fff2d9" stroke-width="4"><path d="M49 25c24 5 1 41-2 53s-2 29-18 23-7-24 0-38 1-42 20-38Z"/><path d="M94 25c24 5 1 41-2 53s-2 29-18 23-7-24 0-38 1-42 20-38Z"/></g>',
'glove':'<path d="M38 88c-17-8-21-32-9-44 9-9 20-6 22-12 9-16 42-9 45 10 17 8 12 29-3 46l-7 16H44z" fill="#e887ad" stroke="#fff2d9" stroke-width="5"/><path d="M43 88h47" stroke="#bc4e7c" stroke-width="7"/>',
'risk':'<path d="m64 23 45 80H19Z" fill="#ffdb39" stroke="#fff2d9" stroke-width="5" stroke-linejoin="round"/><path d="M64 49v25" stroke="#86452e" stroke-width="9" stroke-linecap="round"/><circle cx="64" cy="88" r="5" fill="#86452e"/>',
}
colors={'orbit':'#a885d6','crit':'#f58955','honey':'#ffe99a','summon':'#6aa8e8','bounce':'#e887ad','coin':'#edb448','heart':'#f8b8cb','speed':'#6aa8e8','shield':'#c5a6ec','soda':'#b9e5fb','insoles':'#f4c76c','glove':'#f4a5bd','risk':'#f58955'}
for n,b in shapes.items():save('icon_'+n,badge(colors[n],b))
for name,col in [('cream','#fff2d9'),('purple','#bda0e0'),('blue','#87c7f4'),('coral','#ff857d')]:
 save('panel_'+name,f'<rect x="5" y="9" width="118" height="114" rx="26" fill="#754766" opacity=".18"/><rect x="5" y="4" width="118" height="114" rx="26" fill="{col}" stroke="#fff7e9" stroke-width="5"/><rect x="13" y="12" width="102" height="98" rx="19" fill="#fff2d9" stroke="#edc5a2" stroke-width="2"/>')
for name,col in [('primary','#ff7778'),('secondary','#a885d6'),('disabled','#b4a7b2')]:
 save('button_'+name,f'<rect x="4" y="12" width="120" height="108" rx="30" fill="#795367"/><rect x="4" y="5" width="120" height="108" rx="30" fill="{col}" stroke="#fff2d9" stroke-width="5"/><path d="M22 29q0-9 13-9h58q13 0 13 9" fill="none" stroke="#fff2d9" stroke-opacity=".6" stroke-width="4" stroke-linecap="round"/>')
json.dump({'version':'v01-recovered-20261031','format':'SVG,128x128,transparent,original vector assets','nine_patch_margin':32,'files':[{'name':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(out.glob('*.svg'))]},open(out/'manifest.json','w'),indent=2)
