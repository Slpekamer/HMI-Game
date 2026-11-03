using Godot;
using Hmi.GodotAdapter;
using System;

/// <summary>Cached artistic launch keys, no per-frame ballistic endpoint solver.</summary>
internal sealed class LaunchPosePresentation
{
    private readonly Skeleton3D skeleton;
    private readonly int[] bones;
    private readonly Quaternion[] basis=new Quaternion[7], entry=new Quaternion[7], cancelFrom=new Quaternion[7];
    private readonly Quaternion[][][] keys=new Quaternion[6][][];
    private bool applied,active,cancelling,selected;
    private float age,cancelAge;
    private LaunchAimSample sample;
    private int side,sector;
    internal object Diagnostics{get;private set;}=new{state="idle"};
    internal LaunchPosePresentation(Node root)
    {
        skeleton=Find(root)??throw new InvalidOperationException("Missing launch pose skeleton");
        string[] names={"Chest","UpperArm_L","Forearm_L","Hand_L","UpperArm_R","Forearm_R","Hand_R"};
        bones=Array.ConvertAll(names,n=>skeleton.FindBone(n));if(Array.Exists(bones,x=>x<0))throw new InvalidOperationException("Incomplete launch pose rig");
        for(int s=0;s<2;s++)for(int k=0;k<3;k++)
        {
            float yaw=(s==0?1:-1)*Mathf.DegToRad(k==0?0:k==1?25:30);
            float z=k==0?-.6f:k==1?0:.6f;
            keys[s*3+k]=new[]{BuildKey(s,1,0,yaw),BuildKey(s,k==1?1:.8f,z,yaw)};
        }
    }
    private static Skeleton3D Find(Node n){if(n is Skeleton3D s)return s;foreach(Node c in n.GetChildren()){var found=Find(c);if(found!=null)return found;}return null;}
    private Quaternion[] BuildKey(int s,float outAmount,float z,float yaw)
    {
        var q=new Quaternion[7];for(int i=0;i<7;i++)q[i]=skeleton.GetBoneRest(bones[i]).Basis.Orthonormalized().GetRotationQuaternion();
        Transform3D chestRest=skeleton.GetBoneGlobalRest(bones[0]);
        Basis chestBasis=new Basis(Vector3.Up,yaw)*chestRest.Basis.Orthonormalized();
        int parent=skeleton.GetBoneParent(bones[0]);Basis parentBasis=skeleton.GetBoneGlobalRest(parent).Basis.Orthonormalized();q[0]=(parentBasis.Inverse()*chestBasis).GetRotationQuaternion();
        int index=1+s*3;Transform3D upperRest=skeleton.GetBoneGlobalRest(bones[index]);Transform3D foreRest=skeleton.GetBoneGlobalRest(bones[index+1]);Transform3D handRest=skeleton.GetBoneGlobalRest(bones[index+2]);
        Basis chestDelta=chestBasis*chestRest.Basis.Orthonormalized().Inverse();
        Vector3 shoulder=chestRest.Origin+chestDelta*(upperRest.Origin-chestRest.Origin);
        Vector3 upperDirection=chestDelta*(foreRest.Origin-upperRest.Origin), foreDirection=chestDelta*(handRest.Origin-foreRest.Origin);
        float a=upperDirection.Length(),b=foreDirection.Length(),distance=.92f*(a+b);
        Vector3 axis=new Vector3((s==0?-1:1)*outAmount,-.08f,z).Normalized();
        Vector3 pole=Vector3.Down-axis*Vector3.Down.Dot(axis);pole=pole.Normalized();
        float along=(a*a-b*b+distance*distance)/(2*distance),h2=a*a-along*along;if(h2<=0)throw new InvalidOperationException("Invalid fixed launch key reach");
        Vector3 elbow=shoulder+axis*along+pole*MathF.Sqrt(h2),hand=shoulder+axis*distance;
        Basis upper=new Basis(new Quaternion(upperDirection.Normalized(),(elbow-shoulder).Normalized()))*chestDelta*upperRest.Basis.Orthonormalized();
        Basis fore=new Basis(new Quaternion(foreDirection.Normalized(),(hand-elbow).Normalized()))*chestDelta*foreRest.Basis.Orthonormalized();
        q[index]=(chestBasis.Inverse()*upper).Orthonormalized().GetRotationQuaternion();q[index+1]=(upper.Inverse()*fore).Orthonormalized().GetRotationQuaternion();
        // Preserve the authored wrist rest relation rather than inventing a muzzle axis.
        q[index+2]=skeleton.GetBoneRest(bones[index+2]).Basis.Orthonormalized().GetRotationQuaternion();
        return q;
    }
    internal void Launch(LaunchAimSample launch)
    {
        for(int i=0;i<7;i++)entry[i]=skeleton.GetBonePoseRotation(bones[i]);
        sample=launch;age=0;active=true;selected=false;cancelling=false;
    }
    internal void Cancel()
    {
        if(!active&&!applied)return;
        for(int i=0;i<7;i++)cancelFrom[i]=skeleton.GetBonePoseRotation(bones[i]);
        active=false;cancelling=true;cancelAge=0;
    }
    internal void RestoreBase(){if(!applied||!GodotObject.IsInstanceValid(skeleton))return;for(int i=0;i<7;i++)skeleton.SetBonePoseRotation(bones[i],basis[i]);applied=false;}
    internal void Reset(){RestoreBase();active=cancelling=selected=false;age=cancelAge=0;Diagnostics=new{state="idle"};}
    private static float Smooth(float x){x=Mathf.Clamp(x,0,1);return x*x*(3-2*x);}
    internal void Advance(float dt)
    {
        if(!active&&!cancelling){Diagnostics=new{state="idle"};return;}
        for(int i=0;i<7;i++)basis[i]=skeleton.GetBonePoseRotation(bones[i]);applied=true;
        if(cancelling)
        {
            cancelAge+=dt;float t=Smooth(cancelAge/.06f);for(int i=0;i<7;i++)skeleton.SetBonePoseRotation(bones[i],cancelFrom[i].Slerp(basis[i],t));
            if(cancelAge>=.06f)cancelling=false;Diagnostics=new{state="cancel-to-original-gesture",cancelAge};return;
        }
        if(!selected)
        {
            Vector3 local=skeleton.GlobalBasis.Orthonormalized().Inverse()*new Vector3(sample.Direction.X,0,sample.Direction.Y);float angle=Mathf.Atan2(-local.X,-local.Z);
            side=local.X<0?0:1;sector=Math.Abs(angle)<Mathf.DegToRad(60)?0:Math.Abs(angle)<Mathf.DegToRad(120)?1:2;
            string force=OS.GetEnvironment("LAUNCH_POSE_FORCE");if(force.Length>0){string[] parts=force.Split('-');if(parts.Length!=2)throw new InvalidOperationException("Force format front|side|rear-left|right");sector=parts[0] switch{"front"=>0,"side"=>1,"rear"=>2,_=>throw new InvalidOperationException("Bad forced key")};side=parts[1] switch{"left"=>0,"right"=>1,_=>throw new InvalidOperationException("Bad forced side")};}
            selected=true;
        }
        age+=dt;float sweep=age<.06f?0:age<.16f?Smooth((age-.06f)/.10f):age<.23f?1:1-Smooth((age-.23f)/.05f);
        float enter=Smooth(age/.06f),exit=Smooth((age-.28f)/.08f);var key=keys[side*3+sector];int selectedStart=1+side*3;
        for(int i=0;i<7;i++)
        {
            Quaternion target=i==0 || (i>=selectedStart&&i<selectedStart+3)?key[0][i].Slerp(key[1][i],sweep):basis[i];
            Quaternion posed=entry[i].Slerp(target,enter).Slerp(basis[i],exit);skeleton.SetBonePoseRotation(bones[i],posed);
        }
        Diagnostics=new{state="launch-key-wip",sample.Tick,side,sector,age,sweep,enter,exit,forced=OS.GetEnvironment("LAUNCH_POSE_FORCE"),note="Latched launch-sector energy gesture, not gun/muzzle tracking."};
        if(age>=.36f)active=false;
    }
}
