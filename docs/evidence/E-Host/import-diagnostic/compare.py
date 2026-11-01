from pathlib import Path
import shutil,subprocess,os,json,time
root=Path(__file__).resolve().parent
source=root.parent/'environment-benchmark/project'
records=[]
for index in range(8):
 mode='strict' if index%2==0 else 'delay120'
 dest=root/f'trial-{index}-{mode}'
 shutil.copytree(source,dest/'project',ignore=shutil.ignore_patterns('.godot','bin','obj','*.import','*.uid'))
 env=os.environ.copy();env['XDG_CONFIG_HOME']=str(dest/'config')
 cmd=[env['GODOT_NET'],'--headless','--editor','--path',str(dest/'project'),'--import']
 if mode=='delay120':cmd+=['--quit-after','120']
 start=time.monotonic()
 with (dest/'import.log').open('w') as log:
  try: code=subprocess.run(cmd,env=env,stdout=log,stderr=subprocess.STDOUT,timeout=120).returncode
  except subprocess.TimeoutExpired:code=124
 text=(dest/'import.log').read_text()
 errors=[line for line in text.splitlines() if any(x in line for x in ['ERROR:','Exception','SCRIPT ERROR'])]
 imported=list((dest/'project/.godot/imported').glob('*'))
 row=dict(index=index,mode=mode,exit=code,seconds=round(time.monotonic()-start,3),errors=errors,importedFileCount=len(imported),log=str(dest/'import.log'))
 records.append(row); print(json.dumps(row),flush=True)
 (root/'comparison-results.json').write_text(json.dumps(records,indent=2)+'\n')
