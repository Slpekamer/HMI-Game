"""Compile a caller-supplied frozen legacy reader, not a schema mock."""
from pathlib import Path
import sys,subprocess,tempfile,shutil,hashlib,json,os,argparse,re
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--baseline',required=True,type=Path,help='Clean frozen legacy source checkout')
parser.add_argument('--expected-sha',required=True,help='Exact 40-hex Git commit of that legacy checkout')
args=parser.parse_args()
if not re.fullmatch(r'[0-9a-fA-F]{40}',args.expected_sha):
 parser.error('--expected-sha must be a full 40-hex Git commit')
base=args.baseline.expanduser().resolve()
if not base.is_dir():
 parser.error('--baseline must name an existing legacy source directory')
expected=args.expected_sha.lower()
assert subprocess.check_output(['git','-C',str(base),'rev-parse','HEAD'],text=True).strip()==expected
assert not subprocess.check_output(['git','-C',str(base),'status','--porcelain'],text=True).strip(),'old source must be frozen clean'
work=Path(tempfile.mkdtemp(prefix='hmi-real-old-registry-reader-'));project=work/'old'
for part in ['src/Core','src/Adapters/Persistence']:
 shutil.copytree(base/part,project/part,ignore=shutil.ignore_patterns('bin','obj'))
(project/'content').mkdir();shutil.copy2(base/'content/gameplay.json',project/'content/gameplay.json')
probe=project/'tests/OldReader';probe.mkdir(parents=True)
(probe/'Program.cs').write_bytes((root/'tests/RuleMigrationTests/Compatibility/OldReader.cs.template').read_bytes())
(probe/'OldReader.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><ProjectReference Include="../../src/Adapters/Persistence/Hmi.Persistence.csproj"/><None Include="../../content/gameplay.json" Link="gameplay.json" CopyToOutputDirectory="PreserveNewest"/></ItemGroup></Project>')
(work/'old-source-sha256.json').write_text(json.dumps({str(p.relative_to(project)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((project/'src').rglob('*.cs'))},indent=2))
def run(name,args):
 p=subprocess.run(args,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,timeout=180);(work/(name+'.log')).write_text(p.stdout);print(name,'exit',p.returncode,'evidence',work,flush=True)
 if p.returncode:print(p.stdout);raise SystemExit(1)
 return p.stdout
run('build-old',['dotnet','build',str(probe/'OldReader.csproj'),'-m:1'])
newdll=root/'tests/RuleMigrationTests/bin/Debug/net10.0/Hmi.CoreTests.dll'
assert newdll.exists(),'Build current RuleMigrationTests first'
for label,after in [('pending',1),('selected',4),('selected-closed',-4)]:
 folder=work/label
 run('create-'+label,['dotnet',str(newdll),'--export-preserve-fixture',str(folder),str(after)])
 text=run('old-'+label,['dotnet',str(probe/'bin/Debug/net10.0/OldReader.dll'),str(folder),'pending' if label=='pending' else 'selected'])
 assert 'OLD_READER_SCHEMA3_REJECT PASS' in text
for label,kind in [('schema2-migration','migration'),('schema2-newgame','newgame')]:
 folder=work/label
 run('old-create-'+label,['dotnet',str(probe/'bin/Debug/net10.0/OldReader.dll'),str(folder),'create-'+kind])
 text=run('new-read-'+label,['dotnet',str(newdll),'--inspect-schema2-'+('active' if kind=='migration' else 'pending'),str(folder)])
 assert 'NEW_READER_ACTUAL_SCHEMA2 PASS' in text
print('PASS caller-supplied frozen legacy reader rejects schema3 and current reader accepts actual old schema2 output;',work)
