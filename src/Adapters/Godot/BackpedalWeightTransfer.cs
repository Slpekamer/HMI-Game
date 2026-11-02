using Godot;
using System;

/// <summary>One fixed additive readability pilot. No authority, root motion, or pose replacement.</summary>
internal sealed class BackpedalWeightTransfer
{
    private readonly Skeleton3D _skeleton;
    private readonly int _chest, _head;
    private Quaternion _baseChest, _baseHead;
    private bool _applied;
    public float PitchDegrees { get; private set; }
    public float RollDegrees { get; private set; }
    public float Phase { get; private set; }
    public BackpedalWeightTransfer(Node root)
    {
        Skeleton3D Find(Node node)
        {
            if (node is Skeleton3D skeleton && skeleton.FindBone("Chest") >= 0) return skeleton;
            foreach (Node child in node.GetChildren()) { var hit = Find(child); if (hit != null) return hit; }
            return null;
        }
        _skeleton = Find(root) ?? throw new InvalidOperationException("Weight-transfer skeleton missing");
        _chest = _skeleton.FindBone("Chest"); _head = _skeleton.FindBone("Head");
        if (_head < 0) throw new InvalidOperationException("Weight-transfer Head missing");
    }
    public void RestoreBase()
    {
        if (!_applied || !GodotObject.IsInstanceValid(_skeleton)) return;
        _skeleton.SetBonePoseRotation(_chest, _baseChest);
        _skeleton.SetBonePoseRotation(_head, _baseHead);
        _applied = false;
    }
    private void RotateInSkeletonSpace(int bone, Quaternion delta)
    {
        int parent = _skeleton.GetBoneParent(bone);
        Quaternion parentRotation = parent >= 0
            ? _skeleton.GetBoneGlobalPose(parent).Basis.GetRotationQuaternion() : Quaternion.Identity;
        Quaternion localDelta = parentRotation.Inverse() * delta * parentRotation;
        _skeleton.SetBonePoseRotation(bone, (localDelta * _skeleton.GetBonePoseRotation(bone)).Normalized());
    }
    public void Apply(float clipTime, float movementBlend)
    {
        _baseChest = _skeleton.GetBonePoseRotation(_chest);
        _baseHead = _skeleton.GetBonePoseRotation(_head);
        Phase = Math.Clamp((clipTime - 1f / 30f) / .8f, 0f, 1f);
        float weight = Math.Clamp(movementBlend, 0f, 1f);
        PitchDegrees = -6f * weight; // Forward is skeleton -Z: negative X rotation leans forward.
        // Right foot is airborne first; transfer left at phase .2 and right at phase .7.
        RollDegrees = -5f * MathF.Cos(MathF.Tau * (Phase - .2f)) * weight;
        Quaternion delta = new Quaternion(Vector3.Forward, Mathf.DegToRad(RollDegrees)) *
            new Quaternion(Vector3.Right, Mathf.DegToRad(PitchDegrees));
        RotateInSkeletonSpace(_chest, delta);
        // Head receives half the net added rotation, preserving the existing authored head pose.
        Quaternion halfInverse = Quaternion.Identity.Slerp(delta, .5f).Inverse();
        RotateInSkeletonSpace(_head, halfInverse);
        _applied = true;
    }
}
