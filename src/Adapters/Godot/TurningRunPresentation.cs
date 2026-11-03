using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmi.GodotAdapter;

/// <summary>Opt-in turning Run contact experiment, derived from the frozen straight Run curves. Owns seven lower pose bones only.
/// Phase uses rendered XZ displacement, never input time or authority displacement. No root writes.</summary>
internal sealed class TurningRunPresentation
{
    private const float Step = 1.2f, Contact = .24f, Cycle = 2.4f, Swing = 2.16f;
    private const float ResumeDuration = .05f; // bounded bridge, not a new gait clock
    private const float Half = .12f, Fillet = .048f, Slope = .125f, Lift = .04f, Height = .18f;
    private readonly Node3D _root;
    private readonly Skeleton3D _skeleton;
    private readonly int _pelvis;
    private readonly int[] _owned;
    private readonly Transform3D[] _base = new Transform3D[7];
    private readonly Foot[] _feet;
    private readonly Transform3D _restPelvis;
    private Vector3 _forward;
    private Vector3 _renderVelocity, _lastTravel;
    private Transform3D _previousSkeleton;
    private float _dt, _turnSpreadAge, _turnWeight, _crouch, _crouchVelocity, _releaseAge;
    private bool _requestedMoving, _effectiveMoving;
    private bool _turning, _hasTurned, _turnStop;
    private float _turnAge, _turnBodyOffset, _turnBodyVelocityOffset;
    private int _turnCount;
    private const float TurnDuration = .10f;
    private const float TurnLiftDuration = 1f / 60f;
    private readonly float _scale;
    private Vector3 _lastRoot;
    private float _distance, _phaseOffset, _age, _stopAge, _bodyDelta, _hop, _ground;
    private bool _applied, _started, _stopping, _initialCaptured;
    private int _footfalls, _restartCount;
    private bool _resuming, _tapStopping;
    private float _stopBodyOffset, _stopBodyVelocityOffset;
    private float _resumeAge, _resumeBodyOffset, _resumeBodyVelocityOffset, _bodyVelocity, _lastSpeed;
    private string _failure = "";
    public object Diagnostics { get; private set; } = new { state = "not-started" };

    private sealed class Foot
    {
        public int Thigh, Shin, Bone, Side, CycleIndex = int.MinValue;
        public Transform3D RestThigh, RestShin, RestFoot, Initial, World, Anchor;
        public Vector3[] Sole = Array.Empty<Vector3>();
        public Basis Flat, TurnBasis, StopBasis;
        public float UpperLength, LowerLength, AnkleY, Phase, Margin, SettleAge;
        public bool Stance, Settling, Settled, StopReleasePending;
        public Vector3 SettleRelative, SettleVelocity, Velocity, ResumeOffset, ResumeVelocityOffset, LocalRelative, LocalVelocity;
        public bool BodySpace;
        public bool ResumeHold, TapSettle, TapLiftoff, TurnLift;
        public float TapHorizontalBrake, TapVerticalBrake, TapHeight, TapVerticalVelocity;
        public float ResumeHeight, ResumeVerticalVelocity, ResumeBrakeDuration;
        public float SettleLift;
        public string State = "initial";
    }

    public TurningRunPresentation(Node3D root)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _skeleton = Find(root) ?? throw new InvalidOperationException("Displacement Run skeleton missing");
        _pelvis = Bone("Pelvis"); _restPelvis = _skeleton.GetBoneGlobalRest(_pelvis);
        _scale = _skeleton.GlobalBasis.Scale.X;
        if (Math.Abs(_scale - 1.35f) > .0001f || Math.Abs(_skeleton.GlobalBasis.Scale.Y - _scale) > .0001f || Math.Abs(_skeleton.GlobalBasis.Scale.Z - _scale) > .0001f)
            throw new InvalidOperationException("Displacement Run requires unchanged uniform scale1.35");
        _forward = Horizontal(-_skeleton.GlobalBasis.Z).Normalized(); _lastTravel = _forward;
        _feet = new[] { MakeFoot("L", 0), MakeFoot("R", 1) };
        _owned = new[] { _pelvis, _feet[0].Thigh, _feet[0].Shin, _feet[0].Bone, _feet[1].Thigh, _feet[1].Shin, _feet[1].Bone };
        CollectSoles(root);
        _lastRoot = root.GlobalPosition; _previousSkeleton = _skeleton.GlobalTransform;
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
        foreach (var f in _feet) { f.Initial = _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(f.Bone); f.World = f.Initial; f.Anchor = f.Initial; f.LocalRelative = _skeleton.GlobalBasis.Orthonormalized().Inverse() * (f.World.Origin - _skeleton.GlobalTransform * f.RestThigh.Origin); f.LocalVelocity = Vector3.Zero; }
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
        RestoreBase(); _turning = _hasTurned = _turnStop = false; _turnAge = 0; _turnCount = 0; _renderVelocity = Vector3.Zero; if (GodotObject.IsInstanceValid(_skeleton) && _skeleton.IsInsideTree()) { _forward = Horizontal(-_skeleton.GlobalBasis.Z).Normalized(); _previousSkeleton = _skeleton.GlobalTransform; } _turnSpreadAge = _turnWeight = _crouch = _crouchVelocity = _releaseAge = 0; _requestedMoving = _effectiveMoving = false; _lastTravel = _forward; _turnBodyOffset = _turnBodyVelocityOffset = 0; _distance = _age = _stopAge = _bodyDelta = _hop = 0; _footfalls = 0;
        _started = _stopping = _initialCaptured = _resuming = _tapStopping = false;
        _stopBodyOffset = _stopBodyVelocityOffset = 0;
        _resumeAge = _resumeBodyOffset = _resumeBodyVelocityOffset = _bodyVelocity = _lastSpeed = 0; _restartCount = 0; _failure = ""; _lastRoot = GodotObject.IsInstanceValid(_root) && _root.IsInsideTree() ? _root.GlobalPosition : Vector3.Zero;
        foreach (var f in _feet) { f.CycleIndex = int.MinValue; f.Stance = f.Settling = f.Settled = f.StopReleasePending = false; f.SettleVelocity = f.Velocity = f.ResumeOffset = f.ResumeVelocityOffset = Vector3.Zero; f.ResumeHold = f.TapSettle = f.TapLiftoff = false; f.ResumeHeight = f.ResumeVerticalVelocity = f.ResumeBrakeDuration = 0; f.State = "reset"; }
        Diagnostics = new { state = "reset", distance = 0f };
    }
    public void Advance(float dt, bool moving, Vector3 authoritativeTarget)
    {
        if (!GodotObject.IsInstanceValid(_skeleton) || dt <= 0 || !float.IsFinite(dt)) return;
        if (!_initialCaptured) CaptureInitial();
        Vector3 root = _root.GlobalPosition, displacement = Horizontal(root - _lastRoot); _lastRoot = root;
        float traveled = displacement.Length();
        if (traveled > .15f) Fail("Teleport rendered displacement outside turning contract");
        _dt = dt; _renderVelocity = displacement / dt;
        _requestedMoving = moving;
        _releaseAge = moving ? 0 : _releaseAge + dt;
        // A one-render-step input gap is not a physical stop while the visual root still moves.
        // Continue its displacement phase for at most30ms; authority/input are never changed.
        moving |= _started && traveled / dt > .08f && _releaseAge < .03f;
        _effectiveMoving = moving;
        Vector3 bodyForward = Horizontal(-_skeleton.GlobalBasis.Z).Normalized();
        if (!float.IsFinite(bodyForward.X) || bodyForward.LengthSquared() < .99f) Fail("Invalid turning basis");
        _forward = bodyForward;
        foreach (var f in _feet) f.Flat = _skeleton.GlobalBasis * f.RestFoot.Basis.Orthonormalized();
        bool changed = traveled > .0001f && _lastTravel.Dot(displacement / traveled) < .98480775f;
        if (changed && !_turning && moving) BeginTurn(displacement);
        // A restart must enter before advancing this frame's clocks/width, just like a direction change.
        if (_stopping && moving && _hasTurned) BeginTurn(displacement);
        if (_turning) _turnAge += dt;
        if (_hasTurned) _turnSpreadAge += dt;
        _turnWeight = _hasTurned ? Smooth(_turnSpreadAge / .05f) * (1 - Smooth((_turnSpreadAge - .2f) / .2f)) : 0;
        if (traveled > .0001f && (_turning || !changed)) _lastTravel = displacement / traveled;
        if (!_started && !moving)
        {
            CaptureInitial(); _previousSkeleton = _skeleton.GlobalTransform; Diagnostics = new { state = "idle", distance = _distance, renderDistance = traveled }; return;
        }
        if (!_started) { _started = true; _age = 0; }
        if (_stopping && moving) BeginResume(displacement);
        if (!moving && !_stopping)
        {
            // The turning controller can stop at any travel phase/speed, including four-tick
            // forward starts. Use actual-pose lift/brake/settle, not the fixed pilot's reserved
            // full-speed release fillet. The original fixed-heading Run remains unchanged.
            _turning = false; BeginTapStop(displacement);
        }
        _distance += traveled; _age += dt; if (_stopping) _stopAge += dt; if (_resuming) _resumeAge += dt;
        float previousBodyDelta = _bodyDelta;
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
            if (_turning)
            {
                float t = Math.Clamp(_turnAge / TurnDuration, 0, 1);
                _bodyDelta += (2*t*t*t-3*t*t+1)*_turnBodyOffset
                    + (t*t*t-2*t*t+t)*TurnDuration*_turnBodyVelocityOffset;
            }
            // A short, explicitly authored wider turning step uses modest extra crouch for reach.
            // This is a trajectory choice, never an IK clamp or a planted-anchor correction.
            float crouchTarget = .015f * _turnWeight + (_hasTurned && _tapStopping ? .01f * StopArc() : 0);
            float previousCrouch = _crouch;
            _crouch += (crouchTarget - _crouch) * (1 - MathF.Exp(-30 * dt));
            _crouchVelocity = (_crouch - previousCrouch) / dt;
            _bodyDelta -= _crouch;
            float resumeT = Math.Clamp(_resumeAge / ResumeDuration, 0, 1);
            if (_resuming) _bodyDelta += BridgeOffset(resumeT, _resumeBodyOffset, _resumeBodyVelocityOffset);
            if (_tapStopping && _stopAge < ResumeDuration)
                _bodyDelta += BridgeOffset(_stopAge / ResumeDuration, _stopBodyOffset, _stopBodyVelocityOffset);
            var pelvis = _restPelvis; pelvis.Origin += _skeleton.GlobalBasis.Inverse() * new Vector3(0, _bodyDelta, 0);
            SetGlobal(_pelvis, pelvis);
            foreach (var f in _feet)
            {
                Vector3 previousFootOrigin = f.World.Origin;
                float phase = _distance + _phaseOffset + f.Side * Step;
                int cycle = (int)MathF.Floor(phase / Cycle); float q = Mod(phase, Cycle); f.Phase = q;
                Vector3 hipWorld = _skeleton.GlobalTransform * (f.RestThigh.Origin + _skeleton.GlobalBasis.Inverse() * new Vector3(0, _bodyDelta, 0));
                // A planted stop foot first follows the existing distance-domain release fillet.
                // Its old world anchor is NOT a valid airborne start after the hip passes the rear reach bound.
                bool releaseReady = !f.StopReleasePending || (q >= Contact + Fillet && q < Cycle);
                if (_stopping && !_tapStopping && f.StopReleasePending && !releaseReady && _stopAge >= .2f)
                    Fail("Stop residual displacement insufficient to finish locked-contact release within200ms");
                if (_stopping && !_tapStopping && !f.Settling && !f.Settled && releaseReady)
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
                if (_turning)
                {
                    // A support foot lifts from its original WORLD anchor before horizontal retargeting.
                    // Body yaw never rotates this anchor. Airborne feet brake before joining the run curve.
                    if (f.TurnLift)
                    {
                        f.World = f.Anchor;
                        f.World.Origin += Vector3.Up * (Lift * Smooth(_turnAge / TurnLiftDuration));
                        f.Stance = false; f.State = "turn-liftoff";
                        if (_turnAge >= TurnLiftDuration)
                        {
                            f.TurnLift = false;
                            InitializeTapAir(f, hipWorld, Vector3.Zero, traveled / dt, _turnAge);
                        }
                    }
                    else
                    {
                        float elapsed = _turnAge - f.SettleAge;
                        float duration = TurnDuration - f.SettleAge;
                        if (duration <= Math.Max(f.TapHorizontalBrake, f.TapVerticalBrake)) Fail("No turn interval after braking");
                        Vector3 nominal = NominalFootOrigin(f, _distance, Vector3.Zero);
                        // Lateral clearance has its own 50ms ramp. Do not attenuate it a second time
                        // through the 100ms forward-flight bridge (that crossed boots at other phases).
                        Vector3 width = TurningWidth(f);
                        Basis body = _skeleton.GlobalBasis.Orthonormalized();
                        Vector3 targetRelative = Horizontal(body.Inverse() * (nominal - hipWorld - width));
                        Vector3 bridge = TapHorizontal(f, elapsed, duration, targetRelative);
                        // Re-entry starts from the actual already-separated foot. Retire that old
                        // lateral offset while introducing the new width, rather than adding widths
                        // on every one-tick restart. Forward braking and floor curves are unchanged.
                        bridge.X *= 1 - Smooth(_turnAge / .05f);
                        Vector3 target = hipWorld + body * bridge + width;
                        float tau = f.TapVerticalBrake;
                        if (tau > 0 && elapsed <= tau)
                            target.Y = f.AnkleY + f.TapHeight + f.TapVerticalVelocity * (elapsed - elapsed * elapsed / (2 * tau));
                        else
                            target.Y = Mathf.Lerp(f.AnkleY + f.TapHeight + f.TapVerticalVelocity * tau / 2, nominal.Y,
                                Smooth((elapsed - tau) / (duration - tau)));
                        if (target.Y < f.AnkleY - .00001f) Fail("Turn bridge below preserved floor");
                        // The body heading is already smoothed; airborne shoes travel in its anatomical frame.
                        // Interpolating old WORLD left/right offsets across180 degrees makes them cross.
                        f.World = new Transform3D(f.Flat, target); f.Stance = false; f.State = "turn-bridge";
                    }
                }
                else if (_resuming)
                {
                    bool wasStance = f.Stance;
                    if (f.ResumeHold && q <= Contact)
                    {
                        // Existing planted sole remains the same full world transform.
                        f.World = f.Anchor; f.Stance = true; f.State = "resume-anchor";
                    }
                    else
                    {
                        Vector3 nominal = NominalFootOrigin(f, _distance, Vector3.Zero);
                        Vector3 target = nominal + BridgeOffset(resumeT, f.ResumeOffset, f.ResumeVelocityOffset);
                        if (f.ResumeBrakeDuration > 0) target.Y = ResumeVertical(f, _resumeAge, nominal.Y);
                        if (target.Y < f.AnkleY - .00001f) Fail("Resume bridge would put the rigid sole below its preserved ground");
                        f.World = new Transform3D(f.Flat, target); f.Stance = false; f.State = "resume-bridge";
                        if (resumeT >= 1 && q <= Contact)
                        {
                            f.Anchor = f.World; f.Stance = true; f.State = "stance-anchor";
                            if (!wasStance) _footfalls++;
                        }
                    }
                }
                else if (_tapStopping && (f.TapLiftoff || f.TapSettle))
                {
                    if (f.TapLiftoff)
                    {
                        // With too little residual root travel, a planted tap foot cannot wait forever
                        // for a distance-domain release. Lift from the SAME world anchor first.
                        float liftTime = Fillet / 4.8f;
                        f.World = f.Anchor;
                        f.World.Origin += Vector3.Up * (Lift * Smooth(_stopAge / liftTime));
                        f.Stance = false; f.State = "tap-liftoff";
                        if (_stopAge >= liftTime)
                        {
                            f.TapLiftoff = false;
                            InitializeTapAir(f, hipWorld, Vector3.Zero, traveled / dt, _stopAge);
                        }
                    }
                    if (f.TapSettle)
                    {
                        if (f.Settled) { f.World = f.Anchor; f.Stance = true; f.State = "settled-anchor"; }
                        else
                        {
                            float duration = .2f - f.SettleAge, elapsed = _stopAge - f.SettleAge;
                            if (duration <= Math.Max(f.TapHorizontalBrake, f.TapVerticalBrake)) Fail("No tap settle interval remains after braking");
                            Vector3 goal = StopGoal(f, authoritativeTarget, root); goal.Y = f.AnkleY;
                            Basis frameBasis = f.BodySpace ? _skeleton.GlobalBasis.Orthonormalized() : Basis.Identity;
                            Vector3 relative = TapHorizontal(f, elapsed, duration, Horizontal(frameBasis.Inverse() * (goal - hipWorld)));
                            Vector3 target = hipWorld + frameBasis * relative;
                            if (_hasTurned) target += _skeleton.GlobalBasis.X.Normalized() * ((f.Side == 0 ? -1 : 1) * .04f * StopArc());
                            target.Y = TapVertical(f, elapsed, duration);
                            if (target.Y < f.AnkleY - .00001f) Fail("Tap settle would put rigid sole below preserved ground");
                            f.World = new Transform3D(StopFootBasis(f), target); f.Stance = false; f.State = "tap-settle";
                            if (elapsed >= duration) { f.Anchor = f.World; f.Settled = true; f.Stance = true; f.State = "settled-anchor"; }
                        }
                    }
                }
                else if (f.Settling || f.Settled)
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
                        Vector3 p = hipWorld + _forward * (Half - q) + TurningWidth(f); p.Y = f.AnkleY;
                        f.Anchor = new Transform3D(f.Flat, p); _footfalls++;
                    }
                    f.World = f.Anchor; f.Stance = true; f.State = "stance-anchor";
                }
                else
                {
                    (float r, float lift) = SwingCurve(q - Contact);
                    Vector3 p = hipWorld + _forward * r + TurningWidth(f); p.Y = f.AnkleY + lift;
                    var target = new Transform3D(f.Flat, p);
                    if (_age < .05f && f.CycleIndex == int.MinValue && f.Side == 1)
                        target.Origin = f.Initial.Origin.Lerp(p, start);
                    // First right foot starts in mid-swing: retain transition across successive frames.
                    else if (_age < .05f && f.Side == 1) target.Origin = f.Initial.Origin.Lerp(p, start);
                    f.World = target; f.Stance = false; f.State = "swing";
                }
                f.CycleIndex = cycle;
                Solve(f, hipWorld);
                f.Velocity = (f.World.Origin - previousFootOrigin) / dt;
                Vector3 local = _skeleton.GlobalBasis.Orthonormalized().Inverse() * (f.World.Origin - hipWorld);
                f.LocalVelocity = (local - f.LocalRelative) / dt; f.LocalRelative = local;
            }
            if (_turning && _turnAge >= TurnDuration) _turning = false;
            _bodyVelocity = (_bodyDelta - previousBodyDelta) / dt; _lastSpeed = traveled / dt;
            if (_resuming && _resumeAge >= ResumeDuration) _resuming = false;
            Diagnostics = ReadDiagnostics(traveled, authoritativeTarget);
            _previousSkeleton = _skeleton.GlobalTransform;
        }
        catch (Exception ex)
        {
            _failure = ex.Message; Diagnostics = ReadDiagnostics(traveled, authoritativeTarget); throw;
        }
    }
    private void BeginTurn(Vector3 displacement)
    {
        // Re-enter the original flight curve after a bounded bridge, never reinterpret a rotated
        // world anchor as a valid straight contact. Both feet are airborne during the bridge.
        _phaseOffset = Contact + Fillet - _distance;
        float nominalBody = (_feet[0].AnkleY + Height + NominalHop(_distance)
            - (_skeleton.GlobalTransform * _feet[0].RestThigh.Origin).Y) * Smooth(_age / .05f);
        _turnBodyOffset = (_bodyDelta + _crouch) - nominalBody;
        _turnBodyVelocityOffset = (_bodyVelocity + _crouchVelocity) - NominalHopDerivative(_distance) * _lastSpeed;
        _turning = _hasTurned = true; _turnAge = _turnSpreadAge = 0; _turnCount++;
        _stopping = _tapStopping = _resuming = false;
        foreach (var f in _feet)
        {
            f.TurnLift = !_started || f.Stance; f.TurnBasis = f.World.Basis;
            f.Settling = f.Settled = f.StopReleasePending = f.TapSettle = f.TapLiftoff = f.ResumeHold = false;
            if (f.TurnLift) f.Anchor = f.World;
            else
            {
                Vector3 previousHip = _previousSkeleton * f.RestThigh.Origin + Vector3.Up * _bodyDelta;
                InitializeTapAir(f, previousHip, f.Velocity, _lastSpeed, 0, true);
            }
        }
    }
    private void BeginTapStop(Vector3 currentDisplacement)
    {
        _turnStop = _hasTurned && (_turnWeight > .001f || _forward.Dot(_lastTravel) < .9999f);
        // Only transient stop entry changes. Actual applied pose/velocities/ground survive.
        float nominalBody = _feet[0].AnkleY + Height + NominalHop(_distance)
            - (_skeleton.GlobalTransform * _feet[0].RestThigh.Origin).Y;
        _stopBodyOffset = (_bodyDelta + _crouch) - nominalBody;
        _stopBodyVelocityOffset = (_bodyVelocity + _crouchVelocity) - NominalHopDerivative(_distance) * _lastSpeed;
        _tapStopping = _stopping = true; _resuming = false; _stopAge = 0;
        foreach (var f in _feet)
        {
            f.Settling = f.Settled = f.StopReleasePending = false;
            f.TapSettle = f.TapLiftoff = false; f.StopBasis = f.World.Basis;
            if (f.Stance)
            {
                f.Anchor = f.World; f.TapLiftoff = true;
            }
            else
            {
                Vector3 previousHip = _previousSkeleton * f.RestThigh.Origin + Vector3.Up * _bodyDelta;
                InitializeTapAir(f, previousHip, f.Velocity, _lastSpeed, 0, true);
            }
        }
    }
    private void InitializeTapAir(Foot f, Vector3 hipWorld, Vector3 velocity, float rootSpeed, float startAge, bool previousFrame = false)
    {
        f.TapSettle = true; f.Settling = false; f.Settled = false; f.SettleAge = startAge;
        f.BodySpace = _hasTurned;
        Basis reference = f.BodySpace ? (previousFrame ? _previousSkeleton.Basis : _skeleton.GlobalBasis).Orthonormalized() : Basis.Identity;
        f.SettleRelative = Horizontal(reference.Inverse() * (f.World.Origin - hipWorld));
        Vector3 hipVelocity = Horizontal((_skeleton.GlobalTransform * f.RestThigh.Origin - _previousSkeleton * f.RestThigh.Origin) / _dt);
        f.SettleVelocity = f.BodySpace ? Horizontal(f.LocalVelocity) : Horizontal(velocity) - hipVelocity;
        f.TapHeight = f.World.Origin.Y - f.AnkleY; f.TapVerticalVelocity = velocity.Y;
        // One initial3D corridor bounds BOTH braking clocks, including actual hip vertical velocity.
        // The pose itself is never projected. Curved later targets remain subject to the hard IK guard.
        float brakeCap = ResumeDuration / 2;
        Vector3 relative = reference.Inverse() * (f.World.Origin - hipWorld);
        Vector3 relativeVelocity = f.BodySpace
            ? new Vector3(f.SettleVelocity.X, velocity.Y - _bodyVelocity, f.SettleVelocity.Z)
            : velocity - hipVelocity - Vector3.Up * _bodyVelocity;
        float speed3 = relativeVelocity.Length();
        if (speed3 > .000001f)
        {
            float reach = (f.UpperLength + f.LowerLength) * _scale - .000001f;
            float along = relative.Dot(relativeVelocity / speed3);
            float disc3 = along*along + reach*reach - relative.LengthSquared();
            if (disc3 <= 0) Fail("Tap joint braking has no3D reach corridor");
            float room3 = -along + MathF.Sqrt(disc3);
            if (room3 <= 0) Fail("Tap joint braking starts outside3D reach");
            brakeCap = Math.Min(brakeCap, room3 / speed3);
        }
        f.TapVerticalBrake = Math.Abs(velocity.Y) < .000001f ? 0 : brakeCap;
        if (velocity.Y < 0 && f.TapHeight > 0)
            f.TapVerticalBrake = Math.Min(f.TapVerticalBrake, f.TapHeight / -velocity.Y);
        if (velocity.Y < 0 && f.TapHeight <= 0) Fail("Descending tap foot has no ground clearance");
        // Brake relative horizontal velocity inside half of the currently available reach distance.
        // This changes time, never clamps a pose or scales a limb. Every actual 3D target still passes Solve.
        float speed = f.SettleVelocity.Length(); f.TapHorizontalBrake = 0;
        if (speed > .000001f)
        {
            float length = (f.UpperLength + f.LowerLength) * _scale - .000001f;
            float h = hipWorld.Y - f.World.Origin.Y, radiusSquared = length*length-h*h;
            Vector3 direction = f.SettleVelocity / speed;
            float b = f.SettleRelative.Dot(direction);
            float disc = b*b + radiusSquared - f.SettleRelative.LengthSquared();
            if (disc <= 0) Fail("Tap horizontal braking has no reachable corridor");
            float room = -b + MathF.Sqrt(disc);
            if (room <= 0) Fail("Tap horizontal braking starts outside reach");
            f.TapHorizontalBrake = Math.Min(brakeCap, room / speed);
        }
    }
    private static Vector3 TapHorizontal(Foot f, float elapsed, float duration, Vector3 targetRelative)
    {
        float tau = f.TapHorizontalBrake;
        if (tau > 0 && elapsed <= tau)
            return f.SettleRelative + f.SettleVelocity * (elapsed - elapsed*elapsed/(2*tau));
        Vector3 braked = f.SettleRelative + f.SettleVelocity * (tau/2);
        return braked.Lerp(targetRelative, Smooth((elapsed-tau)/(duration-tau)));
    }
    private static float TapVertical(Foot f, float elapsed, float duration)
    {
        float tau = f.TapVerticalBrake;
        if (tau > 0 && elapsed <= tau)
            return f.AnkleY + f.TapHeight + f.TapVerticalVelocity * (elapsed-elapsed*elapsed/(2*tau));
        float braked = f.TapHeight + f.TapVerticalVelocity*tau/2;
        float t = Smooth((elapsed-tau)/(duration-tau));
        return f.AnkleY + (1-t)*braked + Lift*Square(MathF.Sin(MathF.PI*t));
    }
    private void BeginResume(Vector3 currentDisplacement)
    {
        // These values are the previous actually rendered pose, not the mixer's freshly restored base.
        // Never CaptureInitial here: airborne sole height is not a new ground plane.
        Foot support = _feet.FirstOrDefault(f => f.Stance);
        if (support != null)
        {
            Vector3 previousHip = _skeleton.GlobalTransform * support.RestThigh.Origin - currentDisplacement;
            float r = (support.Anchor.Origin - previousHip).Dot(_forward), q = Half - r;
            if (q < 0 || q > Contact) Fail("Grounded restart anchor lies outside the bounded contact phase");
            _phaseOffset = q - support.Side * Step - _distance;
        }
        float nominalBody = _feet[0].AnkleY + Height + NominalHop(_distance)
            - (_skeleton.GlobalTransform * _feet[0].RestThigh.Origin).Y;
        _resumeBodyOffset = (_bodyDelta + _crouch) - nominalBody;
        _resumeBodyVelocityOffset = (_bodyVelocity + _crouchVelocity) - NominalHopDerivative(_distance) * _lastSpeed;
        foreach (var f in _feet)
        {
            f.ResumeHold = f == support;
            f.ResumeOffset = f.World.Origin - NominalFootOrigin(f, _distance, currentDisplacement);
            // A locked anchor has zero instantaneous velocity; its last landing-frame average is not that velocity.
            Vector3 incoming = f.Stance ? Vector3.Zero : f.Velocity;
            f.ResumeVelocityOffset = incoming - NominalFootVelocity(f, _distance, _lastSpeed);
            f.ResumeHeight = f.World.Origin.Y - f.AnkleY;
            f.ResumeVerticalVelocity = incoming.Y;
            // Stop the actual descent before floor contact, not by clamping the evaluated position.
            // At tau the clearance is h + v*tau/2 >= h/2, with zero vertical speed.
            // Do not restart a25ms brake for harmless tiny descent on every one-tick tap.
            // The ordinary Hermite's first height control point stays >=h/3 when |v|*T<=2h.
            // Only the fast-descent/low-clearance case needs the short analytic braking segment.
            f.ResumeBrakeDuration = incoming.Y < 0 && f.ResumeHeight > 0
                && -incoming.Y * ResumeDuration > 2 * f.ResumeHeight
                ? Math.Min(ResumeDuration / 2, f.ResumeHeight / -incoming.Y) : 0;
            if (f.ResumeHold)
            {
                // Rephasing matches the held support, so it needs no position/velocity bridge.
                if (Horizontal(f.ResumeOffset).Length() > .0001f) Fail("Restart support is not laterally aligned with its hip");
                f.ResumeOffset = f.ResumeVelocityOffset = Vector3.Zero;
            }
            f.Settling = f.Settled = f.StopReleasePending = f.TapSettle = f.TapLiftoff = false;
        }
        _tapStopping = false;
        _resumeAge = 0; _resuming = true; _stopping = false; _restartCount++;
        // _distance, _ground, ankle heights, current bodyDelta and anchors deliberately survive.
    }
    private float StopArc() => _turnStop ? Square(MathF.Sin(MathF.PI * Math.Clamp(_stopAge / .2f, 0, 1))) : 0;
    private Basis StopHeadingRotation()
    {
        float targetYaw = Mathf.Atan2(-_lastTravel.X, -_lastTravel.Z);
        float currentYaw = Mathf.Atan2(-_forward.X, -_forward.Z);
        return new Basis(Vector3.Up, Mathf.AngleDifference(currentYaw, targetYaw) * Smooth(_stopAge / .2f));
    }
    private Vector3 StopGoal(Foot f, Vector3 authority, Vector3 root)
    {
        Vector3 relative = _skeleton.GlobalTransform * f.RestFoot.Origin - root;
        return authority + (_hasTurned ? StopHeadingRotation() * relative : relative);
    }
    private Basis StopFootBasis(Foot f)
    {
        if (!_hasTurned) return f.Flat;
        return StopHeadingRotation() * f.Flat;
    }
    private Vector3 TurningWidth(Foot f) => _skeleton.GlobalBasis.X.Normalized() * ((f.Side == 0 ? -1 : 1) * .04f * _turnWeight);
    private Vector3 NominalFootOrigin(Foot f, float distance, Vector3 rootDisplacement)
    {
        float q = Mod(distance + _phaseOffset + f.Side * Step, Cycle);
        (float r, float lift) = q <= Contact ? (Half - q, 0f) : SwingCurve(q - Contact);
        Vector3 hip = _skeleton.GlobalTransform * f.RestThigh.Origin - rootDisplacement;
        Vector3 target = hip + _forward * r + TurningWidth(f); target.Y = f.AnkleY + lift; return target;
    }
    private Vector3 NominalFootVelocity(Foot f, float distance, float speed)
    {
        float q = Mod(distance + _phaseOffset + f.Side * Step, Cycle);
        if (q <= Contact) return Vector3.Zero;
        return _forward * ((1 + SwingDerivative(q - Contact)) * speed)
            + Vector3.Up * (LiftDerivative(q - Contact) * speed);
    }
    private float NominalHop(float distance)
    {
        float q = Mod(distance + _phaseOffset, Step);
        return q <= Contact ? 0 : .05f * Square(MathF.Sin(MathF.PI * (q - Contact) / (Step - Contact)));
    }
    private float NominalHopDerivative(float distance)
    {
        float q = Mod(distance + _phaseOffset, Step);
        return q <= Contact ? 0 : .05f * MathF.PI / (Step - Contact)
            * MathF.Sin(2 * MathF.PI * (q - Contact) / (Step - Contact));
    }
    private static float LiftDerivative(float u)
    {
        if (u < Fillet) { float t = u / Fillet; return Lift * 6*t*(1-t) / Fillet; }
        if (u > Swing - Fillet) { float t = (Swing-u) / Fillet; return -Lift * 6*t*(1-t) / Fillet; }
        return 0;
    }
    private static float ResumeVertical(Foot f, float age, float nominalY)
    {
        float tau = f.ResumeBrakeDuration;
        if (age <= tau)
            return f.AnkleY + f.ResumeHeight + f.ResumeVerticalVelocity * age
                - f.ResumeVerticalVelocity * age * age / (2 * tau);
        float heightAfterBrake = f.ResumeHeight + f.ResumeVerticalVelocity * tau / 2;
        float t = Smooth((age - tau) / (ResumeDuration - tau));
        // Convex blend of two nonnegative clearances; endpoint inherits the original curve velocity.
        return f.AnkleY + heightAfterBrake + (nominalY - f.AnkleY - heightAfterBrake) * t;
    }
    private static float BridgeOffset(float t, float offset, float velocityOffset) =>
        (2*t*t*t-3*t*t+1)*offset + (t*t*t-2*t*t+t)*ResumeDuration*velocityOffset;
    private static Vector3 BridgeOffset(float t, Vector3 offset, Vector3 velocityOffset) =>
        (2*t*t*t-3*t*t+1)*offset + (t*t*t-2*t*t+t)*ResumeDuration*velocityOffset;
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
        return new { state = _failure.Length>0 ? "failed" : _stopping ? (_tapStopping ? "tap-stopping" : "stopping") : _resuming ? "resuming" : "running", failure = _failure,
            turning = _turning, turnAge = _turnAge, turnCount = _turnCount, turnStepWeight = _turnWeight, turnCrouch = _crouch, turnCrouchVelocity = _crouchVelocity, requestedMoving = _requestedMoving, effectiveMoving = _effectiveMoving, releaseAge = _releaseAge, forward = V(_forward), renderVelocity = V(_renderVelocity), distance = _distance, phase = Mod(_distance+_phaseOffset,Cycle), phaseOffset = _phaseOffset,
            renderDistance = traveled, footfalls = _footfalls, restartCount = _restartCount, resumeAge = _resumeAge, resumeDuration = ResumeDuration, tapStopping = _tapStopping, startAge = _age, stopAge = _stopAge,
            bodyDrop = -_bodyDelta, bodyHop = _hop, height = Height, groundY = _ground,
            root = V(_root.GlobalPosition), authorityTarget = V(authority),
            feet = _feet.Select(f => new { side = f.Side==0?"L":"R", phase=f.Phase, state=f.State, stance=f.Stance,
                reachMargin=f.Margin, worldVelocity=V(f.Velocity), resumeHeldAnchor=f.ResumeHold, resumeOffset=V(f.ResumeOffset), resumeVelocityOffset=V(f.ResumeVelocityOffset), resumeInitialSoleHeight=f.ResumeHeight, resumeInitialVerticalVelocity=f.ResumeVerticalVelocity, resumeBrakeDuration=f.ResumeBrakeDuration, tapHorizontalBrake=f.TapHorizontalBrake, tapVerticalBrake=f.TapVerticalBrake, tapInitialHeight=f.TapHeight, tapInitialVerticalVelocity=f.TapVerticalVelocity, stopReleasePending=f.StopReleasePending, settleAge=f.SettleAge, settleInitialRelativeVelocity=V(f.SettleVelocity), anchor=V(f.Anchor.Origin), ankle=V(f.World.Origin), ankleGroundY=f.AnkleY,
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
