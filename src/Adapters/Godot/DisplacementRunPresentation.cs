using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmi.GodotAdapter;

/// <summary>Isolated fixed-heading Run contact prototype. Owns seven lower pose bones only.
/// Phase uses rendered XZ displacement, never input time or authority displacement. No root writes.</summary>
internal sealed class DisplacementRunPresentation
{
    private const float Step = 1.2f, Contact = .24f, Cycle = 2.4f, Swing = 2.16f;
    private const float Half = .12f, Fillet = .048f, Slope = .125f, Lift = .04f, Height = .18f;
    private readonly Node3D _root;
    private readonly Skeleton3D _skeleton;
    private readonly int _pelvis;
    private readonly int[] _owned;
    private readonly Transform3D[] _base = new Transform3D[7];
    private readonly Foot[] _feet;
    private readonly Transform3D _restPelvis;
    private readonly Vector3 _forward;
    private readonly float _scale;
    private Vector3 _lastRoot;
    private float _distance, _phaseOffset, _age, _stopAge, _bodyDelta, _hop, _ground;
    private bool _applied, _started, _stopping, _initialCaptured;
    private int _footfalls;
    private string _failure = "";
    public object Diagnostics { get; private set; } = new { state = "not-started" };

    private sealed class Foot
    {
        public int Thigh, Shin, Bone, Side, CycleIndex = int.MinValue;
        public Transform3D RestThigh, RestShin, RestFoot, Initial, World, Anchor;
        public Vector3[] Sole = Array.Empty<Vector3>();
        public Basis Flat;
        public float UpperLength, LowerLength, AnkleY, Phase, Margin, SettleAge;
        public bool Stance, Settling, Settled, StopReleasePending;
        public Vector3 SettleRelative, SettleVelocity;
        public float SettleLift;
        public string State = "initial";
    }

    public DisplacementRunPresentation(Node3D root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _skeleton = Find(root) ?? throw new InvalidOperationException("Displacement Run skeleton missing");
        _pelvis = Bone("Pelvis"); _restPelvis = _skeleton.GetBoneGlobalRest(_pelvis);
        _scale = _skeleton.GlobalBasis.Scale.X;
        if (Math.Abs(_scale - 1.35f) > .0001f || Math.Abs(_skeleton.GlobalBasis.Scale.Y - _scale) > .0001f || Math.Abs(_skeleton.GlobalBasis.Scale.Z - _scale) > .0001f)
            throw new InvalidOperationException("Displacement Run requires unchanged uniform scale1.35");
        _forward = Horizontal(-_skeleton.GlobalBasis.Z).Normalized();
        _feet = new[] { MakeFoot("L", 0), MakeFoot("R", 1) };
        _owned = new[] { _pelvis, _feet[0].Thigh, _feet[0].Shin, _feet[0].Bone, _feet[1].Thigh, _feet[1].Shin, _feet[1].Bone };
        CollectSoles(root);
        _lastRoot = root.GlobalPosition;
        CaptureInitial();
    }

    private static Skeleton3D Find(Node node)
    {
        if (node is Skeleton3D skeleton && skeleton.FindBone("Foot_L") >= 0) return skeleton;
        foreach (Node child in node.GetChildren()) { var hit = Find(child); if (hit != null) return hit; }
        return null;
    }
    private int Bone(string name)
    {
        int result = _skeleton.FindBone(name);
        return result >= 0 ? result : throw new InvalidOperationException("Missing bone " + name);
    }
    private Foot MakeFoot(string side, int index)
    {
        var f = new Foot { Thigh = Bone("Thigh_" + side), Shin = Bone("Shin_" + side), Bone = Bone("Foot_" + side), Side = index };
        f.RestThigh = _skeleton.GetBoneGlobalRest(f.Thigh); f.RestShin = _skeleton.GetBoneGlobalRest(f.Shin); f.RestFoot = _skeleton.GetBoneGlobalRest(f.Bone);
        f.UpperLength = f.RestThigh.Origin.DistanceTo(f.RestShin.Origin);
        f.LowerLength = f.RestShin.Origin.DistanceTo(f.RestFoot.Origin);
        f.Flat = _skeleton.GlobalBasis * f.RestFoot.Basis.Orthonormalized();
        return f;
    }
    private void CollectSoles(Node node)
    {
        var points = new Dictionary<int, List<(Vector3 local, float restY)>>();
        foreach (var f in _feet) points[f.Bone] = new();
        void Walk(Node n)
        {
            if (n is MeshInstance3D mesh && mesh.Mesh != null && mesh.Skin != null && mesh.GetNodeOrNull<Skeleton3D>(mesh.Skeleton) == _skeleton)
                for (int surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
                {
                    var a = mesh.Mesh.SurfaceGetArrays(surface); var vertices = a[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                    var bones = a[(int)Mesh.ArrayType.Bones].AsInt32Array(); var weights = a[(int)Mesh.ArrayType.Weights].AsFloat32Array();
                    if (vertices.Length == 0 || bones.Length == 0) continue;
                    int count = bones.Length / vertices.Length;
                    for (int v = 0; v < vertices.Length; v++) for (int w = 0; w < count; w++)
                    {
                        int i = v * count + w; if (weights[i] < .99999f) continue;
                        int bind = bones[i], bone = mesh.Skin.GetBindBone(bind);
                        if (bone < 0) bone = _skeleton.FindBone(mesh.Skin.GetBindName(bind));
                        if (!points.ContainsKey(bone)) continue;
                        Vector3 local = mesh.Skin.GetBindPose(bind) * vertices[v];
                        points[bone].Add((local, (_skeleton.GetBoneGlobalRest(bone) * local).Y));
                    }
                }
            foreach (Node child in n.GetChildren()) Walk(child);
        }
        Walk(node);
        foreach (var f in _feet)
        {
            var list = points[f.Bone]; if (list.Count == 0) throw new InvalidOperationException("No rigid source sole points");
            float min = list.Min(p => p.restY); f.Sole = list.Where(p => p.restY <= min + .0001f).Select(p => p.local).ToArray();
        }
    }
    private void CaptureInitial()
    {
        foreach (var f in _feet) { f.Initial = _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(f.Bone); f.World = f.Initial; f.Anchor = f.Initial; }
        _ground = _feet.Min(f => f.Sole.Min(p => (f.Initial * p).Y));
        foreach (var f in _feet) f.AnkleY = _ground - f.Sole.Min(p => (f.Flat * p).Y);
        Vector3 hip = _skeleton.GlobalTransform * _feet[0].RestThigh.Origin;
        _phaseOffset = Half - (_feet[0].Initial.Origin - hip).Dot(_forward);
        if (_phaseOffset < 0 || _phaseOffset > Contact) throw new InvalidOperationException("Initial Idle Foot is outside initial contact interval");
        _initialCaptured = true;
    }
    public void RestoreBase()
    {
        if (!_applied) return;
        if (!GodotObject.IsInstanceValid(_skeleton) || !_skeleton.IsInsideTree()) { _applied = false; return; }
        for (int i = 0; i < _owned.Length; i++) _skeleton.SetBonePose(_owned[i], _base[i]);
        _applied = false;
    }
    public void Reset()
    {
        RestoreBase(); _distance = _age = _stopAge = _bodyDelta = _hop = 0; _footfalls = 0;
        _started = _stopping = _initialCaptured = false; _failure = ""; _lastRoot = GodotObject.IsInstanceValid(_root) && _root.IsInsideTree() ? _root.GlobalPosition : Vector3.Zero;
        foreach (var f in _feet) { f.CycleIndex = int.MinValue; f.Stance = f.Settling = f.Settled = f.StopReleasePending = false; f.SettleVelocity = Vector3.Zero; f.State = "reset"; }
        Diagnostics = new { state = "reset", distance = 0f };
    }
    public void Advance(float dt, bool moving, Vector3 authoritativeTarget)
    {
        if (!GodotObject.IsInstanceValid(_skeleton) || dt <= 0 || !float.IsFinite(dt)) return;
        if (!_initialCaptured) CaptureInitial();
        Vector3 root = _root.GlobalPosition, displacement = Horizontal(root - _lastRoot); _lastRoot = root;
        if (_forward.Dot(Horizontal(-_skeleton.GlobalBasis.Z).Normalized()) < .99999f) Fail("Fixed-heading prototype cannot turn");
        float traveled = displacement.Length();
        if (traveled > .15f || displacement.Dot(_forward) < -.00001f || (displacement - _forward * displacement.Dot(_forward)).Length() > .0001f)
            Fail("Non-forward/teleport rendered displacement outside prototype contract");
        if (!_started && !moving)
        {
            CaptureInitial(); Diagnostics = new { state = "idle", distance = _distance, renderDistance = traveled }; return;
        }
        if (!_started) { _started = true; _age = 0; }
        if (_stopping && moving) Fail("Restart during settle needs explicit lifecycle Reset in this bounded prototype");
        if (!moving && !_stopping) { _stopping = true; _stopAge = 0; foreach (var f in _feet) f.StopReleasePending = f.Stance; }
        _distance += traveled; _age += dt; if (_stopping) _stopAge += dt;
        for (int i = 0; i < _owned.Length; i++) _base[i] = _skeleton.GetBonePose(_owned[i]);
        _applied = true;
        try
        {
            float start = Smooth(_age / .05f);
            float stepPhase = Mod(_distance + _phaseOffset, Step);
            _hop = stepPhase <= Contact ? 0 : .05f * Square(MathF.Sin(MathF.PI * (stepPhase - Contact) / (Step - Contact)));
            if (_stopping) _hop *= 1 - Smooth(_stopAge / .2f);
            float settleRise = _stopping && _feet.All(f => f.Settled) ? Smooth((_stopAge - .2f) / .1f) : 0;
            float restHipY = (_skeleton.GlobalTransform * _feet[0].RestThigh.Origin).Y;
            float desiredHipY = _feet[0].AnkleY + Height + _hop;
            _bodyDelta = (desiredHipY - restHipY) * start * (1 - settleRise);
            var pelvis = _restPelvis; pelvis.Origin += _skeleton.GlobalBasis.Inverse() * new Vector3(0, _bodyDelta, 0);
            SetGlobal(_pelvis, pelvis);
            foreach (var f in _feet)
            {
                float phase = _distance + _phaseOffset + f.Side * Step;
                int cycle = (int)MathF.Floor(phase / Cycle); float q = Mod(phase, Cycle); f.Phase = q;
                Vector3 hipWorld = _skeleton.GlobalTransform * (f.RestThigh.Origin + _skeleton.GlobalBasis.Inverse() * new Vector3(0, _bodyDelta, 0));
                // A planted stop foot first follows the existing distance-domain release fillet.
                // Its old world anchor is NOT a valid airborne start after the hip passes the rear reach bound.
                bool releaseReady = !f.StopReleasePending || (q >= Contact + Fillet && q < Cycle);
                if (_stopping && f.StopReleasePending && !releaseReady && _stopAge >= .2f)
                    Fail("Stop residual displacement insufficient to finish locked-contact release within200ms");
                if (_stopping && !f.Settling && !f.Settled && releaseReady)
                {
                    f.SettleVelocity = Vector3.Zero;
                    if (f.StopReleasePending)
                    {
                        (float releaseR, float releaseLift) = SwingCurve(q - Contact);
                        Vector3 release = hipWorld + _forward * releaseR; release.Y = f.AnkleY + releaseLift;
                        f.World = new Transform3D(f.Flat, release);
                        f.SettleVelocity = _forward * (SwingDerivative(q - Contact) * traveled / dt);
                    }
                    f.StopReleasePending = false;
                    f.Settling = true; f.SettleAge = _stopAge; f.SettleRelative = Horizontal(f.World.Origin - hipWorld);
                    f.SettleLift = f.World.Origin.Y - f.AnkleY;
                }
                if (f.Settling || f.Settled)
                {
                    // Authority only predicts where the root will finish its unchanged visual Lerp.
                    Vector3 goal = authoritativeTarget + (_skeleton.GlobalTransform * f.RestFoot.Origin - root); goal.Y = f.AnkleY;
                    float duration = .2f - f.SettleAge;
                    if (duration <= 0) Fail("No stop landing interval remains");
                    float rawT = Math.Clamp((_stopAge - f.SettleAge) / duration, 0, 1);
                    float t = Smooth(rawT);
                    Vector3 endRelative = Horizontal(goal - hipWorld);
                    Vector3 target = hipWorld + f.SettleRelative.Lerp(endRelative, t)
                        + f.SettleVelocity * (duration * rawT * Square(1 - rawT));
                    target.Y = f.AnkleY + (1 - t) * f.SettleLift + .04f * Square(MathF.Sin(MathF.PI * t));
                    if (f.Settled) target = f.Anchor.Origin;
                    f.World = new Transform3D(f.Flat, target); f.Stance = false; f.State = "stop-swing";
                    if (t >= 1 || f.Settled) { f.Settled = true; f.Settling = false; f.Anchor = f.World; f.Stance = true; f.State = "settled-anchor"; }
                }
                else if (q <= Contact)
                {
                    // Initial left contact is the actual pre-movement Idle foot, never a +.12 teleport.
                    if (f.CycleIndex == int.MinValue && f.Side == 0) f.Anchor = f.Initial;
                    else if (!f.Stance || cycle != f.CycleIndex)
                    {
                        Vector3 p = hipWorld + _forward * (Half - q); p.Y = f.AnkleY;
                        f.Anchor = new Transform3D(f.Flat, p); _footfalls++;
                    }
                    f.World = f.Anchor; f.Stance = true; f.State = "stance-anchor";
                }
                else
                {
                    (float r, float lift) = SwingCurve(q - Contact);
                    Vector3 p = hipWorld + _forward * r; p.Y = f.AnkleY + lift;
                    var target = new Transform3D(f.Flat, p);
                    if (_age < .05f && f.CycleIndex == int.MinValue && f.Side == 1)
                        target.Origin = f.Initial.Origin.Lerp(p, start);
                    // First right foot starts in mid-swing: retain transition across successive frames.
                    else if (_age < .05f && f.Side == 1) target.Origin = f.Initial.Origin.Lerp(p, start);
                    f.World = target; f.Stance = false; f.State = "swing";
                }
                f.CycleIndex = cycle;
                Solve(f, hipWorld);
            }
            Diagnostics = ReadDiagnostics(traveled, authoritativeTarget);
        }
        catch (Exception ex)
        {
            _failure = ex.Message; Diagnostics = ReadDiagnostics(traveled, authoritativeTarget); throw;
        }
    }
    private void Solve(Foot f, Vector3 hipWorld)
    {
        Transform3D inv = _skeleton.GlobalTransform.AffineInverse();
        Vector3 hip = inv * hipWorld, ankle = inv * f.World.Origin, v = ankle - hip;
        float d = v.Length(), l1 = f.UpperLength, l2 = f.LowerLength;
        f.Margin = (l1 + l2 - d) * _scale;
        if (!float.IsFinite(d) || d <= Math.Abs(l1 - l2) + .000001f || d >= l1 + l2 - .000001f)
            Fail($"Unreachable {f.Side}: distance={d:R}, length={l1+l2:R}, phase={f.Phase:R}, age={_age:R}, stop={_stopAge:R}");
        Vector3 axis = v / d;
        Vector3 pole = Vector3.Forward - axis * Vector3.Forward.Dot(axis);
        if (pole.LengthSquared() < .000001f) Fail("Degenerate knee pole");
        pole = pole.Normalized();
        float along = (l1*l1-l2*l2+d*d)/(2*d), heightSquared = l1*l1-along*along;
        if (heightSquared <= 0) Fail("Nonpositive IK knee height; no clamping permitted");
        Vector3 knee = hip + axis * along + pole * MathF.Sqrt(heightSquared);
        SetGlobal(f.Thigh, Aim(f.RestThigh, f.RestShin.Origin-f.RestThigh.Origin, hip, knee));
        SetGlobal(f.Shin, Aim(f.RestShin, f.RestFoot.Origin-f.RestShin.Origin, knee, ankle));
        Transform3D foot = inv * f.World; foot.Basis = foot.Basis.Orthonormalized(); SetGlobal(f.Bone, foot);
    }
    private static Transform3D Aim(Transform3D rest, Vector3 direction, Vector3 head, Vector3 tail)
    {
        Quaternion delta = new Quaternion(direction.Normalized(), (tail-head).Normalized());
        return new Transform3D(new Basis(delta) * rest.Basis.Orthonormalized(), head);
    }
    private void SetGlobal(int bone, Transform3D pose)
    {
        int parent = _skeleton.GetBoneParent(bone);
        Transform3D local = parent < 0 ? pose : _skeleton.GetBoneGlobalPose(parent).AffineInverse() * pose;
        _skeleton.SetBonePosePosition(bone, local.Origin);
        _skeleton.SetBonePoseRotation(bone, local.Basis.Orthonormalized().GetRotationQuaternion());
        _skeleton.SetBonePoseScale(bone, Vector3.One);
    }
    private object ReadDiagnostics(float traveled, Vector3 authority)
    {
        return new { state = _failure.Length>0 ? "failed" : _stopping ? "stopping" : "running", failure = _failure,
            distance = _distance, phase = Mod(_distance+_phaseOffset,Cycle), phaseOffset = _phaseOffset,
            renderDistance = traveled, footfalls = _footfalls, startAge = _age, stopAge = _stopAge,
            bodyDrop = -_bodyDelta, bodyHop = _hop, height = Height, groundY = _ground,
            root = V(_root.GlobalPosition), authorityTarget = V(authority),
            feet = _feet.Select(f => new { side = f.Side==0?"L":"R", phase=f.Phase, state=f.State, stance=f.Stance,
                reachMargin=f.Margin, stopReleasePending=f.StopReleasePending, settleAge=f.SettleAge, settleInitialRelativeVelocity=V(f.SettleVelocity), anchor=V(f.Anchor.Origin), ankle=V(f.World.Origin), ankleGroundY=f.AnkleY,
                solePositions=f.Sole.Select(p=>V((_skeleton.GlobalTransform*_skeleton.GetBoneGlobalPose(f.Bone))*p)).ToArray() }).ToArray() };
    }
    private void Fail(string message) { _failure = message; throw new InvalidOperationException("DisplacementRun: " + message); }
    private static (float r,float lift) SwingCurve(float u)
    {
        if (u < Fillet) { float t=u/Fillet; return (-Half-u+(1+Slope)*u*u/(2*Fillet),Lift*Smooth(t)); }
        if (u > Swing-Fillet) { float v=Swing-u,t=v/Fillet;return (-(-Half-v+(1+Slope)*v*v/(2*Fillet)),Lift*Smooth(t)); }
        float width=Swing-2*Fillet,t0=(u-Fillet)/width;
        float p0=-Half-Fillet+(1+Slope)*Fillet/2,p1=-p0;
        float h00=2*t0*t0*t0-3*t0*t0+1,h10=t0*t0*t0-2*t0*t0+t0,h01=-2*t0*t0*t0+3*t0*t0,h11=t0*t0*t0-t0*t0;
        return (h00*p0+h10*width*Slope+h01*p1+h11*width*Slope,Lift);
    }
    private static float SwingDerivative(float u)
    {
        if (u < Fillet) return -1 + (1 + Slope) * u / Fillet;
        if (u > Swing - Fillet) return -1 + (1 + Slope) * (Swing - u) / Fillet;
        float width = Swing - 2 * Fillet, t = (u - Fillet) / width;
        float p0 = -Half - Fillet + (1 + Slope) * Fillet / 2, p1 = -p0;
        return ((6*t*t-6*t)*p0 + (3*t*t-4*t+1)*width*Slope
            + (-6*t*t+6*t)*p1 + (3*t*t-2*t)*width*Slope) / width;
    }
    private static float Smooth(float x) { x=Math.Clamp(x,0,1);return x*x*(3-2*x); }
    private static float Mod(float x,float period) => x-MathF.Floor(x/period)*period;
    private static float Square(float x)=>x*x;
    private static Vector3 Horizontal(Vector3 p)=>new(p.X,0,p.Z);
    private static float[] V(Vector3 p)=>new[]{p.X,p.Y,p.Z};
}
