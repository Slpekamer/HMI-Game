using Godot;
using Hmi.Core;
using System;
using System.Collections.Generic;
namespace Hmi.GodotAdapter;
// Cosmetic event-direction symbol, never weapon path/contact/range or authority.
internal sealed class CommonMeleeCue:IDisposable
{
 internal const float Duration=.16f;
 readonly MeshInstance3D node; readonly ArrayMesh fan,petal; readonly StandardMaterial3D material; readonly Camera3D camera;
 readonly Vector3[] fanVertices,petalVertices;
 readonly GameEvent[] pending=new GameEvent[8];readonly int[] indices=new int[8];readonly Queue<double> onsets=new();
 GameSimulation simulation; EntityId player; int run=-1,wave=-1,lastIndex=-1,count,lastDrop;long lastTick=-1;
 bool disposed,ended,active,darkOpportunity=true,dropBaseline;double clock;float age;GameEvent current;int currentIndex;
 internal int ScopeDrops{get;private set;}
 internal int PresentationDrops{get;private set;}internal int PausedDrops{get;private set;}
 internal int Observed{get;private set;}internal int Started{get;private set;}internal int ActiveDrops{get;private set;}internal int RateDrops{get;private set;}internal int DarkDrops{get;private set;}internal int BudgetDrops{get;private set;}internal int ShortDrops{get;private set;}internal int InvalidDrops{get;private set;}internal int LossDrops{get;private set;}internal int DuplicateDrops{get;private set;}
 internal bool Active=>active;internal float Age=>age;internal double Clock=>clock;internal int Pending=>count;internal int RollingOnsets=>onsets.Count;
 internal string Signature=>clock.ToString("R")+"|"+active+"|"+age.ToString("R")+"|"+count+"|"+onsets.Count;
 internal readonly record struct CueSnapshot(int Run,int Wave,long Tick,int Index,string Source,string Target,float Age,bool Visible,bool NormalDepth,bool Petal,Transform3D Transform);
 internal CueSnapshot Snapshot()=>new(run,wave,current.Tick,currentIndex,current.Source.ToString(),current.Target.ToString(),age,active&&!disposed&&!node.IsQueuedForDeletion(),!material.NoDepthTest,(current.To-current.From).Length<.45f,node.Transform);
 internal CommonMeleeCue(Node3D owner,Camera3D camera){this.camera=camera;fan=Create(false,out fanVertices);petal=Create(true,out petalVertices);material=new StandardMaterial3D{VertexColorUseAsAlbedo=true,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,CullMode=BaseMaterial3D.CullModeEnum.Disabled,NoDepthTest=false,AlbedoColor=new Color(1,1,1,.72f)};node=new MeshInstance3D{Name="BasicMeleeEventSymbol",Mesh=fan,MaterialOverride=material,Visible=false,CustomAabb=new Aabb(new Vector3(-1,0,-1),new Vector3(2,.0121f,2)),CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};owner.AddChild(node);}
 void Scope(int r,int w,EntityId p){if(run==r&&wave==w&&player==p)return;Hide();ScopeDrops+=count;count=0;run=r;wave=w;player=p;lastTick=-1;lastIndex=-1;clock=0;onsets.Clear();darkOpportunity=true;}
 internal void BeginFrame(int r,int w,EntityId p){if(disposed)return;ended=false;Scope(r,w,p);}
 internal void CaptureStep(GameSimulation g){if(disposed||g==null)return;if(!ReferenceEquals(simulation,g)){simulation=g;run=-1;dropBaseline=false;}Scope(g.RunToken,g.Wave,g.PlayerId);bool loss=dropBaseline?g.DroppedEvents!=lastDrop:g.DroppedEvents!=0;lastDrop=g.DroppedEvents;dropBaseline=true;
 for(int i=0;i<g.EventCount;i++)Capture(g.GetEvent(i),i,g.Tick,loss);}
 internal bool Capture(GameEvent e,int index,long step,bool loss=false){if(disposed||e.Kind!=EventKind.Melee||e.Mechanism!=FeedbackMechanism.BasicMelee||e.Source!=player||e.RunToken!=run||e.Wave!=wave||e.Tick!=step||e.Tick<0||index<0)return false;
 if(e.Tick<lastTick||(e.Tick==lastTick&&index<=lastIndex)){DuplicateDrops++;return false;}lastTick=e.Tick;lastIndex=index;Observed++;
 if(loss){LossDrops++;return false;}if(!Finite(e.From)||!Finite(e.To)||Math.Abs(e.From.X)>GameSimulation.ArenaWidth/2||Math.Abs(e.To.X)>GameSimulation.ArenaWidth/2||Math.Abs(e.From.Y)>GameSimulation.ArenaHeight/2||Math.Abs(e.To.Y)>GameSimulation.ArenaHeight/2){InvalidDrops++;return false;}
 float length=(e.To-e.From).Length;if(!float.IsFinite(length)){InvalidDrops++;return false;}if(length<.18f){ShortDrops++;return false;}if(count==pending.Length){BudgetDrops++;return false;}pending[count]=e;indices[count]=index;count++;return true;}
 internal void EndFrame(float delta,bool paused,bool visible){if(disposed||ended)return;ended=true;if(!GodotObject.IsInstanceValid(node)||!GodotObject.IsInstanceValid(camera)){ClearPresentation();return;}
 if(!visible){ClearPresentation();return;}if(paused){PausedDrops+=count;count=0;return;}
 float dt=float.IsFinite(delta)&&delta>0?Math.Min(delta,.25f):0;clock+=dt;while(onsets.Count>0&&clock-onsets.Peek()>=1)onsets.Dequeue();
 if(active){age+=dt;if(age>=Duration){Hide();darkOpportunity=false;}else material.AlbedoColor=new Color(1,1,1,.72f*(1-age/Duration));}
 bool born=false;for(int i=0;i<count;i++){if(active){ActiveDrops++;continue;}if(born){BudgetDrops++;continue;}if(!darkOpportunity){DarkDrops++;continue;}if(onsets.Count>=2){RateDrops++;continue;}Spawn(pending[i],indices[i]);onsets.Enqueue(clock);Started++;born=true;}
 count=0;if(!active)darkOpportunity=true;
 }
 void Spawn(GameEvent e,int index){current=e;currentIndex=index;age=0;float length=(e.To-e.From).Length;bool compact=length<.45f;var f=new Vector3(e.To.X-e.From.X,0,e.To.Y-e.From.Y).Normalized();var center=new Vector3(e.From.X,.65f,e.From.Y)+f*Math.Min(.30f,.35f*length);var n=camera.GlobalPosition-center;n-=f*n.Dot(f);if(n.LengthSquared()<1e-8f)n=Vector3.Up;n=n.Normalized();var side=f.Cross(n).Normalized();
 float halfDepth=compact?Math.Min(.08f,.22f*length):Math.Min(.175f,.22f*length);float width=compact?Math.Min(.22f,.6f*length):Math.Min(.50f,1.1f*length);
 node.Mesh=compact?petal:fan;node.Transform=new Transform3D(new Basis(f*halfDepth,n,side*(width*.5f)),center);material.AlbedoColor=new Color(1,1,1,.72f);node.Visible=true;active=true;darkOpportunity=false;
 }
 static bool Finite(Vec2 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y);
 void Hide(){active=false;age=0;if(GodotObject.IsInstanceValid(node))node.Visible=false;}
 internal void ClearPresentation(){if(disposed)return;Hide();PresentationDrops+=count;count=0;darkOpportunity=false; /* same-scope highwater, clock and rolling onsets deliberately retained */}
 public void Dispose(){if(disposed)return;ClearPresentation();disposed=true;if(GodotObject.IsInstanceValid(node)){node.Mesh=null;node.MaterialOverride=null;node.QueueFree();}fan.Dispose();petal.Dispose();material.Dispose();}
 internal Vector3[] ProjectedVertices(){var source=(current.To-current.From).Length<.45f?petalVertices:fanVertices;var result=new Vector3[source.Length];for(int i=0;i<result.Length;i++)result[i]=node.Transform*source[i];return result;}
 static ArrayMesh Create(bool compact,out Vector3[] authored){var vertices=new List<Vector3>();var colors=new List<Color>();
 void Tri(Vector3 a,Vector3 b,Vector3 c,Color ca,Color cb,Color cc){var cross=(b-a).Cross(c-a);if(cross.LengthSquared()<1e-12f)return;if(cross.Y>=0)throw new Exception("Melee fan winding/face invalid");foreach(var v in new[]{a,b,c})if(Math.Abs(v.X)>1||Math.Abs(v.Z)>1||v.Y<0||v.Y>.0121)throw new Exception("Melee geometry outside declared bounds");vertices.AddRange(new[]{a,b,c});colors.AddRange(new[]{ca,cb,cc});}
 float[] z=compact?new[]{-1f,0,1}:new[]{-1f,-.5f,0,.5f,1};float[] front=compact?new[]{-.65f,1,-.65f}:new[]{-.70f,.55f,1,.55f,-.70f};float[] back=compact?new[]{-.65f,-.65f,-.65f}:new[]{-.70f,-.28f,-.50f,-.25f,-.70f};var rings=new Vector3[z.Length][];Color[] palette={new("705184"),new("D2ADEE"),new("EAD6F4"),new("A17BC7"),new("705184")};
 for(int k=0;k<z.Length;k++){float width=front[k]-back[k],t=1-Math.Abs(z[k]);rings[k]=new[]{new Vector3(back[k],0,z[k]),new Vector3(back[k]+width*.06f,.002f*t,z[k]),new Vector3(back[k]+width*.62f,.012f*t,z[k]),new Vector3(front[k]-width*.06f,.002f*t,z[k]),new Vector3(front[k],0,z[k])};}
 for(int k=0;k<rings.Length-1;k++)for(int j=0;j<4;j++){var a=rings[k][j];var b=rings[k][j+1];var c=rings[k+1][j+1];var d=rings[k+1][j];Tri(a,b,c,palette[j],palette[j+1],palette[j+1]);Tri(a,c,d,palette[j],palette[j+1],palette[j]);}
 using var arrays=new Godot.Collections.Array();arrays.Resize((int)Mesh.ArrayType.Max);arrays[(int)Mesh.ArrayType.Vertex]=vertices.ToArray();arrays[(int)Mesh.ArrayType.Color]=colors.ToArray();authored=vertices.ToArray();var result=new ArrayMesh();result.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);return result;}
}
