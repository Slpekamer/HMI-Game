using Godot;
using Hmi.GodotAdapter;
using System;

/// <summary>Opt-in upper-body yaw only; never changes root, bone positions, scale or authority.</summary>
internal sealed class MovingLaunchAimPresentation
{
    private readonly Skeleton3D skeleton;
    private readonly int chest;
    private readonly int[] arms, hands, elbows;
    private readonly float[] planeWeight = new float[2];
    private readonly Quaternion[] original = new Quaternion[3];
    private readonly float[] armYaw = new float[2];
    private bool applied;
    private float chestYaw, weight;
    private int side;
    private long launchTick = -1;
    private float aimWorldYaw;
    private bool aimHeadingKnown;
    internal object Diagnostics { get; private set; } = new { state="idle" };
    internal MovingLaunchAimPresentation(Node root)
    {
        skeleton=Find(root) ?? throw new InvalidOperationException("Missing aim skeleton");
        chest=skeleton.FindBone("Chest");
        arms=new[]{skeleton.FindBone("UpperArm_L"),skeleton.FindBone("UpperArm_R")};
        hands=new[]{skeleton.FindBone("Hand_L"),skeleton.FindBone("Hand_R")};
        elbows=new[]{skeleton.FindBone("Forearm_L"),skeleton.FindBone("Forearm_R")};
        if(chest<0 || Array.Exists(arms,x=>x<0) || Array.Exists(hands,x=>x<0) || Array.Exists(elbows,x=>x<0)) throw new InvalidOperationException("Missing aim chain");
    }
    private static Skeleton3D Find(Node n){if(n is Skeleton3D s)return s;foreach(Node c in n.GetChildren()){var found=Find(c);if(found!=null)return found;}return null;}
    internal void RestoreBase()
    {
        if(!applied || !GodotObject.IsInstanceValid(skeleton))return;
        skeleton.SetBonePoseRotation(chest,original[0]);
        for(int i=0;i<2;i++)skeleton.SetBonePoseRotation(arms[i],original[i+1]);
        applied=false;
    }
    internal void Reset(){RestoreBase();weight=chestYaw=0;armYaw[0]=armYaw[1]=planeWeight[0]=planeWeight[1]=0;launchTick=-1;aimHeadingKnown=false;Diagnostics=new{state="idle"};}
    internal void Advance(float dt, LaunchAimSample? sample, long tick, bool attackActive)
    {
        bool valid=sample is LaunchAimSample a && tick>=a.Tick && tick-a.Tick<=60;
        float targetWeight=valid&&attackActive?1:0;
        weight=Mathf.MoveToward(weight,targetWeight,dt/.08f);
        if(weight<=0){planeWeight[0]=planeWeight[1]=0;aimHeadingKnown=false;chestYaw=0;armYaw[0]=armYaw[1]=0;Diagnostics=new{state="idle"};return;}
        if(!valid)return;
        var aim=sample.Value;
        Vector3 worldDirection=new(aim.Direction.X,0,aim.Direction.Y);
        Vector3 local=skeleton.GlobalBasis.Orthonormalized().Inverse()*worldDirection;
        float desired=Mathf.Atan2(-local.X,-local.Z);
        if(launchTick!=aim.Tick){launchTick=aim.Tick;int next=local.X<0?0:1;if(next!=side)aimHeadingKnown=false;side=next;}
        chestYaw=Mathf.MoveToward(chestYaw,Mathf.Clamp(desired,-Mathf.Pi/3,Mathf.Pi/3),dt*Mathf.DegToRad(360));
        original[0]=skeleton.GetBonePoseRotation(chest);
        for(int i=0;i<2;i++)original[i+1]=skeleton.GetBonePoseRotation(arms[i]);
        applied=true;
        Yaw(chest,chestYaw*weight);
        Vector3 sourceReach=skeleton.GlobalBasis*(skeleton.GetBoneGlobalPose(hands[side]).Origin-skeleton.GetBoneGlobalPose(arms[side]).Origin);
        if(!aimHeadingKnown){aimWorldYaw=Mathf.Atan2(-sourceReach.X,-sourceReach.Z);aimHeadingKnown=true;}
        float requestedWorldYaw=Mathf.Atan2(-worldDirection.X,-worldDirection.Z);
        aimWorldYaw+=Mathf.Clamp(Mathf.AngleDifference(aimWorldYaw,requestedWorldYaw),-dt*Mathf.DegToRad(720),dt*Mathf.DegToRad(720));
        Vector3 smoothLocal=skeleton.GlobalBasis.Orthonormalized().Inverse()*new Vector3(-Mathf.Sin(aimWorldYaw),0,-Mathf.Cos(aimWorldYaw));
        float smoothDesired=Mathf.Atan2(-smoothLocal.X,-smoothLocal.Z);
        float error=0;float poleAlignment=0;
        for(int i=0;i<2;i++)
        {
            Vector3 shoulder=skeleton.GetBoneGlobalPose(arms[i]).Origin;
            Vector3 hand=skeleton.GetBoneGlobalPose(hands[i]).Origin;
            Vector3 reach=hand-shoulder; reach.Y=0;
            float target=0;
            if(i==side && reach.LengthSquared()>1e-6f)
            {
                float current=Mathf.Atan2(-reach.X,-reach.Z);
                target=Mathf.AngleDifference(current,smoothDesired);
                target=Mathf.Clamp(target,-Mathf.DegToRad(170),Mathf.DegToRad(170));
            }
            // Smooth the world-space aim target, not the inverse animation correction.
            // Smoothing that correction makes fast authored arm motion create persistent aim lag.
            armYaw[i]=i==side?target:Mathf.LerpAngle(armYaw[i],0,1-Mathf.Exp(-25*dt));
            Yaw(arms[i],armYaw[i]*weight);
            planeWeight[i]=Mathf.MoveToward(planeWeight[i],i==side?1:0,dt/.08f);
            float alignment=OutwardElbow(i,planeWeight[i]*weight);
            if(i==side)poleAlignment=alignment;
            if(i==side)
            {
                Vector3 actual=skeleton.GetBoneGlobalPose(hands[i]).Origin-skeleton.GetBoneGlobalPose(arms[i]).Origin;
                actual.Y=0;
                error=actual.LengthSquared()>1e-6f?Mathf.Abs(Mathf.AngleDifference(Mathf.Atan2(-actual.X,-actual.Z),desired)):Mathf.Pi;
            }
        }
        Diagnostics=new{state="upper-yaw-wip",launchTick,side,weight,chestYaw,armYaw=new[]{armYaw[0],armYaw[1]},desired,aimWorldYaw,poleAlignment,reachHeadingError=error,note="Shoulder-to-hand horizontal proxy, not a muzzle or authoritative launch origin."};
    }
    private float OutwardElbow(int i,float blend)
    {
        Vector3 shoulder=skeleton.GetBoneGlobalPose(arms[i]).Origin;
        Vector3 hand=skeleton.GetBoneGlobalPose(hands[i]).Origin;
        Vector3 elbow=skeleton.GetBoneGlobalPose(elbows[i]).Origin;
        Vector3 axis=(hand-shoulder).Normalized();
        Vector3 current=elbow-shoulder;current-=axis*current.Dot(axis);
        Vector3 outward=skeleton.GetBoneGlobalPose(chest).Basis.Orthonormalized().X*(i==0?-1:1)+Vector3.Down*.35f;
        Vector3 target=outward-axis*outward.Dot(axis);
        if(current.LengthSquared()<1e-9f || target.LengthSquared()<1e-9f)return 0;
        current=current.Normalized();target=target.Normalized();
        float angle=Mathf.Atan2(axis.Dot(current.Cross(target)),current.Dot(target));
        Rotate(arms[i],axis,angle*blend);
        Vector3 after=skeleton.GetBoneGlobalPose(elbows[i]).Origin-shoulder;after-=axis*after.Dot(axis);
        return after.Normalized().Dot(target);
    }
    private void Yaw(int bone,float angle)=>Rotate(bone,Vector3.Up,angle);
    private void Rotate(int bone,Vector3 axis,float angle)
    {
        int parent=skeleton.GetBoneParent(bone);
        Basis parentBasis=parent<0?Basis.Identity:skeleton.GetBoneGlobalPose(parent).Basis.Orthonormalized();
        Basis global=skeleton.GetBoneGlobalPose(bone).Basis.Orthonormalized();
        skeleton.SetBonePoseRotation(bone,(parentBasis.Inverse()*new Basis(axis,angle)*global).Orthonormalized().GetRotationQuaternion());
    }
}
