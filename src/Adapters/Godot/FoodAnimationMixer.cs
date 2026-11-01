using Godot;
using System;
using System.Collections.Generic;

namespace Hmi.GodotAdapter;

/// <summary>Candidate instance-owned native animation graph. No authority or root motion.</summary>
internal sealed class FoodAnimationMixer : IDisposable
{
    private readonly AnimationTree _tree;
    private float _movement;
    private bool _disposed;
    public bool AttackActive => !_disposed && GodotObject.IsInstanceValid(_tree) && _tree.Get("parameters/UpperAttack/active").AsBool();
    public int FilteredTracks { get; }
    private FoodAnimationMixer(AnimationTree tree,int tracks) { _tree=tree;FilteredTracks=tracks; }
    public static FoodAnimationMixer TryCreate(AnimationPlayer player,string idle,string run,string attack)
    {
        if(player==null || !player.IsInsideTree() || !player.HasAnimation(idle) || !player.HasAnimation(run) || !player.HasAnimation(attack)) return null;
        var allowed=new HashSet<string>(StringComparer.Ordinal) { "Chest","Head","Scarf","UpperArm_L","UpperArm_R","Forearm_L","Forearm_R","Hand_L","Hand_R" };
        var shot=new AnimationNodeOneShot { FadeInTime=.06,FadeOutTime=.08,FilterEnabled=true,MixMode=AnimationNodeOneShot.MixModeEnum.Blend };
        var clip=player.GetAnimation(attack);int tracks=0;
        for(int i=0;i<clip.GetTrackCount();i++)
        {
            var path=clip.TrackGetPath(i);
            if(path.GetSubNameCount()!=1) continue;
            string bone=path.GetSubName(0);
            if(!allowed.Contains(bone)) continue;
            // Exact imported path and actual Skeleton bone; never match Root/Pelvis by substring.
            Node root=player.GetNode(player.RootNode);
            var skeleton=root.GetNodeOrNull<Skeleton3D>(new NodePath(path.GetConcatenatedNames()));
            if(skeleton==null || skeleton.FindBone(bone)<0) continue;
            shot.SetFilterPath(path,true);tracks++;
        }
        if(tracks<3) { shot.Dispose(); return null; }
        var graph=new AnimationNodeBlendTree();
        graph.AddNode("Idle",new AnimationNodeAnimation { Animation=idle });
        graph.AddNode("Run",new AnimationNodeAnimation { Animation=run });
        graph.AddNode("RunRate",new AnimationNodeTimeScale());
        graph.AddNode("Locomotion",new AnimationNodeBlend2());
        graph.AddNode("Attack",new AnimationNodeAnimation { Animation=attack });
        graph.AddNode("UpperAttack",shot);
        graph.AddNode("ResetClock",new AnimationNodeTimeSeek());
        graph.ConnectNode("RunRate",0,"Run");graph.ConnectNode("Locomotion",0,"Idle");graph.ConnectNode("Locomotion",1,"RunRate");
        graph.ConnectNode("UpperAttack",0,"Locomotion");graph.ConnectNode("UpperAttack",1,"Attack");graph.ConnectNode("ResetClock",0,"UpperAttack");graph.ConnectNode("output",0,"ResetClock");
        var tree=new AnimationTree { Name="FoodLocomotionMixer",TreeRoot=graph,CallbackModeProcess=AnimationMixer.AnimationCallbackModeProcess.Manual,RootNode=player.RootNode };
        player.GetParent().AddChild(tree);tree.AnimPlayer=tree.GetPathTo(player);player.Stop();tree.Active=true;
        tree.Set("parameters/RunRate/scale",1.0);tree.Advance(0);
        return new FoodAnimationMixer(tree,tracks);
    }
    public void Advance(float delta,bool moving,float runRate,bool fire,bool paused)
    {
        if(_disposed || !GodotObject.IsInstanceValid(_tree) || !_tree.IsInsideTree() || paused || delta<=0 || !float.IsFinite(delta)) return;
        delta=Math.Min(delta,.25f);
        _movement=Mathf.MoveToward(_movement,moving?1:0,delta/.10f);
        _tree.Set("parameters/Locomotion/blend_amount",_movement);
        _tree.Set("parameters/RunRate/scale",float.IsFinite(runRate)?Math.Clamp(runRate,.25f,3f):1f);
        if(fire && !AttackActive) _tree.Set("parameters/UpperAttack/request",(int)AnimationNodeOneShot.OneShotRequest.Fire);
        _tree.Advance(delta);
    }
    public void Reset()
    {
        // Scene exit can dispose a presenter after its child mixer has left the tree.
        // Do not evaluate animation against detached Skeleton targets.
        if(_disposed || !GodotObject.IsInstanceValid(_tree) || !_tree.IsInsideTree())return;
        _movement=0;_tree.Set("parameters/Locomotion/blend_amount",0f);
        _tree.Set("parameters/UpperAttack/request",(int)AnimationNodeOneShot.OneShotRequest.Abort);
        _tree.Set("parameters/ResetClock/seek_request",0.0);_tree.Advance(0);
    }
    public void Dispose()
    {
        if(_disposed)return;_disposed=true;
        if(GodotObject.IsInstanceValid(_tree)) { _tree.Active=false;_tree.QueueFree(); }
    }
}
