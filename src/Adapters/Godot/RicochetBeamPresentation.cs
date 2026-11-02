using Godot;
using Hmi.Core;
using System;
using System.Collections.Generic;
namespace Hmi.GodotAdapter;
/// <summary>Observed instantaneous Ricochet segments. No simulation mutation, actor aiming or delayed flight.</summary>
internal sealed class RicochetBeamPresentation:IDisposable
{
 internal const int Capacity=8,MaxLinks=5,PendingCapacity=8,PerFrame=2;
 internal const float Duration=.16f;
 sealed class Group {internal readonly GameEvent[] Events=new GameEvent[MaxLinks];internal int Count;internal long Tick;internal int FirstIndex;}
 sealed class Burst {internal readonly MeshInstance3D[] Nodes=new MeshInstance3D[MaxLinks];internal StandardMaterial3D Material;internal int DecorationCount;internal MultiMesh Decoration;internal MultiMeshInstance3D DecorationNode;internal bool Active;internal float Age;internal int Count;internal readonly GameEvent[] Events=new GameEvent[MaxLinks];}
 readonly Group[] pending=new Group[PendingCapacity];readonly Burst[] bursts=new Burst[Capacity];readonly GameEvent[] scan=new GameEvent[MaxLinks];readonly Node3D container;readonly Camera3D camera;readonly ArrayMesh mesh;readonly ArrayMesh crystalMesh;readonly ArrayMesh shortRibbonMesh;readonly bool facetShading=true;readonly bool ribbon=true;readonly bool hasBackplate=false;readonly bool crystals=false;
 GameSimulation simulation;int run=-1,wave=-1,pendingCount,lastIndex=-1,lastDrop;long lastTick=-1;bool dropBaseline,disposed,ended;float tokens=2;double clock;
 internal int Started{get;private set;}internal int CapturedGroups{get;private set;}internal int CapturedLinks{get;private set;}internal int DuplicateSteps{get;private set;}internal int InvalidGroups{get;private set;}internal int EventLossSteps{get;private set;}internal int EventLossGroups{get;private set;}internal int BudgetDrops{get;private set;}internal int PendingDrops{get;private set;}internal int StartsThisFrame{get;private set;}
 internal bool HasFacetShading=>facetShading;internal bool HasRibbon=>ribbon;internal bool HasBackplate=>hasBackplate;internal bool HasCrystals=>crystals;
 internal int ActiveCount{get{int n=0;foreach(var b in bursts)if(b.Active)n++;return n;}}
 internal int ActiveLinks{get{int n=0;foreach(var b in bursts)if(b.Active)n+=b.Count;return n;}}
 internal int PendingCount=>pendingCount;internal double Clock=>clock;
 internal string StateSignature {get{var b=new System.Text.StringBuilder(clock.ToString("R"));foreach(var s in bursts)if(s.Active)b.Append("|").Append(s.Age.ToString("R")).Append(":").Append(s.Count);return b.ToString();}}
 internal readonly record struct LinkSnapshot(long Tick,string Source,string Target,Vector3 From,Vector3 To,float Age,float Alpha,bool Visible,bool NormalDepth,Vector3 ActualStart,Vector3 ActualEnd,float ProjectedBodyWidth,bool Ribbon,bool ShortRibbon);
 internal LinkSnapshot[] Snapshot(){var l=new List<LinkSnapshot>();foreach(var b in bursts)if(b.Active)for(int i=0;i<b.Count;i++){var e=b.Events[i];l.Add(new(e.Tick,e.Source.ToString(),e.Target.ToString(),Point(e.From),Point(e.To),b.Age,b.Material.AlbedoColor.A,b.Nodes[i].Visible,!b.Material.NoDepthTest,b.Nodes[i].GlobalPosition,b.Nodes[i].GlobalTransform*Vector3.Back,camera.UnprojectPosition(b.Nodes[i].GlobalTransform*new Vector3(ribbon?.14f:.06f,0,.3f)).DistanceTo(camera.UnprojectPosition(b.Nodes[i].GlobalTransform*new Vector3(ribbon?-.14f:-.06f,0,.3f))),ribbon,ribbon&&(Point(e.To)-Point(e.From)).Length()<.9f));}return l.ToArray();}
 internal RicochetBeamPresentation(Node3D owner,Camera3D camera){this.camera=camera;container=new Node3D{Name="RicochetBeamPool"};owner.AddChild(container);mesh=ribbon?CreateRibbon(false,facetShading):CreateBeam(hasBackplate);shortRibbonMesh=ribbon?CreateRibbon(true,facetShading):null;crystalMesh=CreateCrystal();for(int i=0;i<PendingCapacity;i++)pending[i]=new Group();for(int i=0;i<Capacity;i++){var b=new Burst{Material=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,VertexColorUseAsAlbedo=true,NoDepthTest=false,CullMode=BaseMaterial3D.CullModeEnum.Disabled}};bursts[i]=b;if(crystals){b.Decoration=new MultiMesh{TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,Mesh=crystalMesh,CustomAabb=new Aabb(new Vector3(-GameSimulation.ArenaWidth/2-.2f,.35f,-GameSimulation.ArenaHeight/2-.2f),new Vector3(GameSimulation.ArenaWidth+.4f,.4f,GameSimulation.ArenaHeight+.4f)),InstanceCount=15,VisibleInstanceCount=0};b.DecorationNode=new MultiMeshInstance3D{Multimesh=b.Decoration,MaterialOverride=b.Material,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};container.AddChild(b.DecorationNode);}for(int j=0;j<MaxLinks;j++){b.Nodes[j]=new MeshInstance3D{Name=$"RicochetBeam_{i}_{j}",Mesh=mesh,MaterialOverride=b.Material,Visible=false,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};container.AddChild(b.Nodes[j]);}}}
 static Vector3 Point(Vec2 p)=>new(p.X,.55f,p.Y);
 static bool Finite(Vec2 p)=>float.IsFinite(p.X)&&float.IsFinite(p.Y);
 void Scope(int r,int w){if(r==run&&w==wave)return;ClearPresentation();run=r;wave=w;lastTick=-1;lastIndex=-1;clock=0;tokens=2;}
 internal void BeginFrame(int r,int w){if(disposed)return;ended=false;StartsThisFrame=0;Scope(r,w);}
 internal void CaptureStep(GameSimulation g){if(disposed||g==null)return;
 if(!ReferenceEquals(simulation,g)){ClearPresentation();simulation=g;run=-1;wave=-1;lastTick=-1;lastIndex=-1;dropBaseline=false;}Scope(g.RunToken,g.Wave);
 int dropped=g.DroppedEvents;bool loss=dropBaseline?dropped!=lastDrop:dropped!=0;dropBaseline=true;lastDrop=dropped;
 int count=0,first=-1;bool invalid=false,oldMember=false,newMember=false;
 for(int i=0;i<g.EventCount;i++){
  var e=g.GetEvent(i);if(e.Kind!=EventKind.Ray||e.Mechanism!=FeedbackMechanism.Ricochet||e.Source!=g.PlayerId||e.RunToken!=run||e.Wave!=wave||e.Tick!=g.Tick)continue;
  if(e.Tick<lastTick||(e.Tick==lastTick&&i<=lastIndex)){oldMember=true;continue;}newMember=true;if(first<0)first=i;
  if(count>=MaxLinks){invalid=true;count++;continue;}
  float length=(Point(e.To)-Point(e.From)).Length();if(!Finite(e.From)||!Finite(e.To)||Math.Abs(e.From.X)>GameSimulation.ArenaWidth/2||Math.Abs(e.To.X)>GameSimulation.ArenaWidth/2||Math.Abs(e.From.Y)>GameSimulation.ArenaHeight/2||Math.Abs(e.To.Y)>GameSimulation.ArenaHeight/2||!float.IsFinite(e.Value)||e.Value<=0||e.Target.Index<0||e.Target.Index>=g.Capacity||e.Target.Generation<0||!float.IsFinite(length)||length>40)invalid=true;
  if(count>0&&(scan[count-1].To.X!=e.From.X||scan[count-1].To.Y!=e.From.Y))invalid=true;
  scan[count++]=e;
 }
 if(newMember){lastTick=g.Tick;lastIndex=g.EventCount-1;}else if(oldMember)DuplicateSteps++;
 if(loss){EventLossSteps++;if(newMember)EventLossGroups++;return;}
 if(!newMember)return;if(oldMember||invalid||count>MaxLinks){InvalidGroups++;return;}
 if(g.Paused||g.Phase!=RunPhase.Combat)return;
 CapturedGroups++;CapturedLinks+=count;if(pendingCount>=PendingCapacity){PendingDrops++;return;}
 var group=pending[pendingCount++];group.Count=count;group.Tick=g.Tick;group.FirstIndex=first;Array.Copy(scan,group.Events,count);
 }
 internal void EndFrame(float delta,bool paused,bool combat){if(disposed||ended)return;ended=true;if(!combat){ClearPresentation();return;}if(paused){pendingCount=0;return;}float dt=float.IsFinite(delta)?Math.Clamp(delta,0,.25f):0;clock+=dt;tokens=Math.Min(2,tokens+6*dt);
 foreach(var b in bursts)if(b.Active){b.Age+=dt;if(b.Age>=Duration)Hide(b);}
 // CaptureStep order is authoritative Step/event order; never queue budget-excess groups.
 for(int p=0;p<pendingCount;p++){var g=pending[p];if(StartsThisFrame>=PerFrame||tokens<1){BudgetDrops++;continue;}Burst slot=null;foreach(var b in bursts)if(!b.Active){slot=b;break;}if(slot==null){BudgetDrops++;continue;}slot.Active=true;slot.Age=0;slot.Count=g.Count;Array.Copy(g.Events,slot.Events,g.Count);tokens--;StartsThisFrame++;Started++;}
 pendingCount=0;foreach(var b in bursts)if(b.Active)Draw(b);
 }
 void Draw(Burst b){float ratio=1-b.Age/Duration;b.Material.AlbedoColor=new Color(1,1,1,.90f*ratio);float width=.75f+.25f*ratio;
 int decorationCount=0;for(int i=0;i<b.Count;i++){var e=b.Events[i];Vector3 from=Point(e.From),to=Point(e.To),v=to-from;float length=v.Length();var node=b.Nodes[i];if(length<.001f){node.Visible=false;continue;}Vector3 direction=v/length,normal=camera.GlobalPosition-(from+to)*.5f;normal-=direction*normal.Dot(direction);if(normal.LengthSquared()<.000001f)normal=Vector3.Up;normal=normal.Normalized();Vector3 side=normal.Cross(direction).Normalized();normal=direction.Cross(side).Normalized();float transverse=ribbon&&length<.9f?Math.Min(1,length*.35f/.28f):1;if(ribbon)node.Mesh=length<.9f?shortRibbonMesh:mesh;node.GlobalTransform=new Transform3D(new Basis(side*width*transverse,normal*width,direction*length),from);node.Visible=true;if(crystals){int count=CrystalCount(length);for(int j=0;j<count;j++){Vector3 center=from+v*((j+1f)/(count+1));b.Decoration.SetInstanceTransform(decorationCount++,new Transform3D(new Basis(side*width,normal*width,direction),center));}}}
 if(crystals){b.DecorationCount=decorationCount;b.Decoration.VisibleInstanceCount=decorationCount;}
 }
 void Hide(Burst b){b.DecorationCount=0;if(b.Decoration!=null)b.Decoration.VisibleInstanceCount=0;b.Active=false;b.Age=0;b.Count=0;foreach(var n in b.Nodes)if(GodotObject.IsInstanceValid(n))n.Visible=false;}
 internal void ClearPresentation(){if(disposed)return;pendingCount=0;foreach(var b in bursts)if(b!=null)Hide(b);}
 public void Dispose(){if(disposed)return;ClearPresentation();disposed=true;foreach(var b in bursts){foreach(var n in b.Nodes)if(GodotObject.IsInstanceValid(n)){n.Mesh=null;n.MaterialOverride=null;}if(b.DecorationNode!=null){b.DecorationNode.Multimesh=null;b.DecorationNode.MaterialOverride=null;b.Decoration.Mesh=null;b.Decoration.Dispose();}b.Material.Dispose();}mesh.Dispose();crystalMesh.Dispose();shortRibbonMesh?.Dispose();if(GodotObject.IsInstanceValid(container))container.QueueFree();}

 internal static int CrystalCount(float length)=>length<.65f?0:length<1.8f?1:length<3?2:3;
 internal int ActiveCrystalCount{get{int n=0;foreach(var b in bursts)if(b.Decoration!=null)n+=b.DecorationCount;return n;}}

 static ArrayMesh CreateRibbon(bool shortForm,bool shading){var verts=new List<Vector3>();var colors=new List<Color>();void Tri(Vector3 a,Vector3 b,Vector3 c,Color ca,Color cb,Color cc){var cross=(b-a).Cross(c-a);if(cross.LengthSquared()<1e-12f)return;if(cross.Y>=0)throw new InvalidOperationException("ribbon triangle folded/inverted");foreach(var v in new[]{a,b,c})if(v.Z<0||v.Z>1||Math.Abs(v.X)>.17f||v.Y<0||v.Y>.01201f)throw new InvalidOperationException("ribbon geometry outside declared bounds");verts.Add(a);verts.Add(b);verts.Add(c);colors.Add(ca);colors.Add(cb);colors.Add(cc);}void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color ca,Color cb,Color cc,Color cd){Tri(a,b,c,ca,cb,cc);Tri(a,c,d,ca,cc,cd);}
 float[] t=shortForm?new[]{0f,.25f,.5f,.75f,1f}:new[]{0f,.12f,.30f,.52f,.72f,.90f,1f};float[] w=shortForm?new[]{0f,.6f,1f,.6f,0f}:new[]{0f,.35f,1f,.72f,.9f,.3f,0f};float[] offset=shortForm?new float[5]:new[]{0f,.018f,.025f,0f,-.018f,-.012f,0f};
 float[] high=shortForm?new[]{0f,.7f,1f,.7f,0f}:new[]{0f,.2f,.85f,1f,.85f,.2f,0f};Color[][] vertexColors=new Color[t.Length][];for(int i=0;i<t.Length;i++)vertexColors[i]=new[]{new Color("207D8D"),new Color("43C9CC"),new Color("43C9CC").Lerp(new Color("88DDD9"),high[i]),new Color("238C9B"),new Color("207D8D")};
 var rings=new Vector3[t.Length][];for(int i=0;i<t.Length;i++){float half=.14f*w[i],edge=Math.Min(.008f,half*.2f),center=offset[i];rings[i]=new[]{new Vector3(center-half,0,t[i]),new Vector3(center-half+edge,.002f*w[i],t[i]),new Vector3(center+half*.45f,.012f*w[i],t[i]),new Vector3(center+half-edge,.002f*w[i],t[i]),new Vector3(center+half,0,t[i])};}
 for(int k=0;k<t.Length-1;k++)for(int j=0;j<4;j++){Color col=j==0||j==3?new Color("176A78"):j==2&&k>=1&&k<=3?new Color("75DDD7"):new Color("20BFC7");Quad(rings[k][j],rings[k][j+1],rings[k+1][j+1],rings[k+1][j],shading?vertexColors[k][j]:col,shading?vertexColors[k][j+1]:col,shading?vertexColors[k+1][j+1]:col,shading?vertexColors[k+1][j]:col);}
 using var a=new Godot.Collections.Array();a.Resize((int)Mesh.ArrayType.Max);a[(int)Mesh.ArrayType.Vertex]=verts.ToArray();a[(int)Mesh.ArrayType.Color]=colors.ToArray();var result=new ArrayMesh();result.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,a);return result;}
 static ArrayMesh CreateCrystal(){var v=new List<Vector3>();var c=new List<Color>();void Tri(Vector3 a,Vector3 b,Vector3 d,Color col){v.Add(a);v.Add(b);v.Add(d);c.Add(col);c.Add(col);c.Add(col);}
 Vector3[] outer={new(0,0,-.14f),new(.11f,0,0),new(0,0,.14f),new(-.11f,0,0)};Vector3[] inner={new(0,.003f,-.12f),new(.09f,.003f,0),new(0,.003f,.12f),new(-.09f,.003f,0)};Color[] col={new("9ceaf0"),new("52bfce"),new("67d8dc"),new("b8f1e9")};
 for(int i=0;i<4;i++){int j=(i+1)%4;Tri(outer[i],outer[j],inner[j],new Color("49335f"));Tri(outer[i],inner[j],inner[i],new Color("49335f"));Tri(inner[i],inner[j],new Vector3(0,.025f,0),col[i]);}
 using var a=new Godot.Collections.Array();a.Resize((int)Mesh.ArrayType.Max);a[(int)Mesh.ArrayType.Vertex]=v.ToArray();a[(int)Mesh.ArrayType.Color]=c.ToArray();var m=new ArrayMesh();m.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,a);return m;}
 static ArrayMesh CreateBeam(bool includeBackplate){var v=new List<Vector3>();var colors=new List<Color>();void Tri(Vector3 a,Vector3 b,Vector3 c,Color color){v.Add(a);v.Add(b);v.Add(c);colors.Add(color);colors.Add(color);colors.Add(color);}void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color){Tri(a,b,c,color);Tri(a,c,d,color);}
 float[] z={0,.1f,.5f,.9f,1},w={.15f,.85f,1,.85f,.15f},x={-.06f,-.045f,0,.045f,.06f},y={0,.018f,.025f,.018f,0};Color[] palette={new("49335f"),new("52bfce"),new("9ceaf0"),new("49335f")};
 for(int k=0;k<4;k++){for(int j=0;j<4;j++)Quad(new(x[j]*w[k],y[j],z[k]),new(x[j+1]*w[k],y[j+1],z[k]),new(x[j+1]*w[k+1],y[j+1],z[k+1]),new(x[j]*w[k+1],y[j],z[k+1]),palette[j]);if(includeBackplate)Quad(new(-.06f*w[k],-.018f,z[k]),new(.06f*w[k],-.018f,z[k]),new(.06f*w[k+1],-.018f,z[k+1]),new(-.06f*w[k+1],-.018f,z[k+1]),new Color("49335f"));for(int side=-1;side<=1;side+=2)Quad(new(side*.06f*w[k],-.018f,z[k]),new(side*.06f*w[k],0,z[k]),new(side*.06f*w[k+1],0,z[k+1]),new(side*.06f*w[k+1],-.018f,z[k+1]),new Color("49335f"));}
 using var arrays=new Godot.Collections.Array();arrays.Resize((int)Mesh.ArrayType.Max);arrays[(int)Mesh.ArrayType.Vertex]=v.ToArray();arrays[(int)Mesh.ArrayType.Color]=colors.ToArray();var result=new ArrayMesh();result.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);return result;}
}
