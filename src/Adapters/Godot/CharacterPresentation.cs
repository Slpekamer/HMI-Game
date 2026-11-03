using Godot;
using Hmi.Core;
using Hmi.GodotAdapter;
using System;
using System.Collections.Generic;

/// <summary>Instance-owned cosmetics only. Never writes simulation state or schedules damage.</summary>
public sealed class CharacterPresentation : IDisposable
{
    private readonly Node3D _root;
    private readonly AnimationPlayer _player;
    private readonly FoodAnimationMixer _mixer;
    private readonly bool _controlledGait;
    private AnimationLibrary _gaitLibrary;
    private Animation _gaitClip;
    private readonly BackpedalWeightTransfer _weightTransfer;
    private readonly DisplacementRunPresentation _displacementRun;
    private readonly TurningRunPresentation _turningRun;
    private readonly MovingLaunchAimPresentation _movingAim;
    private readonly LaunchPosePresentation _launchPose;
    internal object LaunchPoseDiagnostics => _launchPose?.Diagnostics;
    internal void CancelLaunchCue(){_launchAim=null;_movingAim?.Reset();_launchPose?.Cancel();}
    internal object MovingAimDiagnostics => _movingAim?.Diagnostics;
    internal void ClearLaunchAim() { _launchAim=null; _movingAim?.Reset(); _launchPose?.Reset(); }
    private readonly DisplacementBackpedalPresentation _displacementBackpedal;
    private readonly DisplacementSideHopPresentation _displacementSideHop;
    private readonly EntityId _entity;
    private readonly string _idle, _run, _attack;
    private readonly FacialBinding[] _face;
    private Vector3 _target, _previousSample;
    private long _tick = -1, _lastAttackTick = -1;
    private int _runToken, _wave;
    private float _speed, _attackRemaining, _facing, _health, _travelFacing, _headingBridgeRemaining;
    private float _faceClock, _smileRemaining, _surpriseRemaining;
    private string _current = "";
    private bool _attackQueued, _disposed;
    private LaunchAimSample? _launchAim;
    internal LaunchAimSample? LaunchAim => _launchAim;
    internal bool AcceptLaunchAim(LaunchAimSample sample)
    {
        if (!Alive() || sample.Owner != _entity || sample.RunToken != _runToken || sample.Wave != _wave || sample.Tick > _tick || sample.Tick < 0 || (_launchAim is LaunchAimSample old && sample.Tick <= old.Tick)) return false;
        _launchAim = sample; _launchPose?.Launch(sample); return true;
    }

    private readonly struct FacialBinding
    {
        public readonly MeshInstance3D Mesh;
        public readonly int Index, Channel;
        public readonly bool HasBlink;
        public FacialBinding(MeshInstance3D mesh, int index, int channel, bool hasBlink)
        { Mesh = mesh; Index = index; Channel = channel; HasBlink = hasBlink; }
    }

    public bool IsDisposed => _disposed;
    public bool HasFacialShapes => _face.Length > 0;
    public string CurrentClip => _current;

    public CharacterPresentation(Node3D root, EntityView view, long tick, int runToken = 0, int wave = 0)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _entity = view.Id; _runToken = runToken; _wave = wave;
        _player = FindPlayer(root);
        _idle = FindClip("Idle"); _run = FindClip("Run"); _attack = FindClip("Attack");
        _controlledGait = ControlledGaitDrill.Enabled && view.Kind == EntityKind.Player && FindClip("StrafeR").Length > 0;
        if (_player != null)
        {
            // One clock owns clip progression. Godot's automatic process must not race pause/morphs.
            _player.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual;
            if (_idle.Length > 0) _player.GetAnimation(_idle).LoopMode = Animation.LoopModeEnum.Linear;
            if (_run.Length > 0) _player.GetAnimation(_run).LoopMode = Animation.LoopModeEnum.Linear;
            if (_attack.Length > 0) _player.GetAnimation(_attack).LoopMode = Animation.LoopModeEnum.None;
        }
        var bindings = new List<FacialBinding>();
        CollectFace(root, bindings); _face = bindings.ToArray();
        _target = _previousSample = new Vector3(view.Position.X, 0, view.Position.Y);
        _root.Position = _target; _tick = tick; _health = view.Health;
        SetFacing(view.Facing); _root.Rotation = new Vector3(0, _controlledGait ? ControlledGaitDrill.Yaw : _facing, 0);
        string mixerRun = _controlledGait ? CreateGaitComposite() : _run;
        _mixer=FoodAnimationMixer.TryCreate(_player,_idle,mixerRun,_attack);
        if (_controlledGait && ControlledGaitDrill.WeightTransfer) _weightTransfer = new BackpedalWeightTransfer(_root);
        if (_controlledGait && ControlledGaitDrill.DisplacementRun)
        {
            if (ControlledGaitDrill.TurningRun) _turningRun = new TurningRunPresentation(_root);
            else if (ControlledGaitDrill.Clip == "StrafeR" || ControlledGaitDrill.Clip == "StrafeL")
                _displacementSideHop = new DisplacementSideHopPresentation(_root, ControlledGaitDrill.Clip == "StrafeL" ? -1 : 1);
            else if (ControlledGaitDrill.Clip == "Backpedal") _displacementBackpedal = new DisplacementBackpedalPresentation(_root);
            else _displacementRun = new DisplacementRunPresentation(_root);
        }
        if (_controlledGait && ControlledGaitDrill.TurningRun && OS.GetEnvironment("MOVING_LAUNCH_AIM")=="1") _movingAim=new MovingLaunchAimPresentation(_root);
        if (_controlledGait && ControlledGaitDrill.TurningRun && OS.GetEnvironment("MOVING_LAUNCH_POSE")=="1")
        { if(_movingAim!=null)throw new InvalidOperationException("Choose one experimental upper-body owner");_launchPose=new LaunchPosePresentation(_root); }
        ResetTransient();
    }

    // Asset-drill-only composition. The static switch is set exclusively by ControlledGaitMovie.
    // Duplicate Run and replace exactly the seven lower bones; original resources stay untouched.
    private string CreateGaitComposite()
    {
        var original = _player.GetAnimation(_run);
        // The frozen asset has no StrafeL clip. Use its existing lateral base only;
        // the opt-in displacement component owns the candidate lower pose.
        string sourceName = FindClip(ControlledGaitDrill.Clip == "StrafeL" ? "StrafeR" : ControlledGaitDrill.Clip);
        if (sourceName.Length == 0) throw new InvalidOperationException("Missing directional drill clip");
        var source = _player.GetAnimation(sourceName);
        if (Math.Abs(original.Length - source.Length) > .00001)
            throw new InvalidOperationException("Drill clip period differs from original Run");
        _gaitClip = (Animation)original.Duplicate(true);
        _gaitClip.LoopMode = Animation.LoopModeEnum.Linear;
        bool Lower(Animation clip, int track)
        {
            var path = clip.TrackGetPath(track);
            return path.GetSubNameCount() == 1 && ControlledGaitDrill.LowerBones.Contains(path.GetSubName(0));
        }
        if (ControlledGaitDrill.Candidate)
        {
            for (int i = _gaitClip.GetTrackCount() - 1; i >= 0; i--)
                if (Lower(_gaitClip, i)) _gaitClip.RemoveTrack(i);
            var found = new HashSet<string>();
            int tracks = 0;
            for (int i = 0; i < source.GetTrackCount(); i++)
                if (Lower(source, i))
                {
                    source.CopyTrack(i, _gaitClip);
                    found.Add(source.TrackGetPath(i).GetSubName(0));
                    tracks++;
                }
            if (found.Count != 7)
                throw new InvalidOperationException($"Expected seven lower bones (track count may differ after rebuild), got {found.Count}/{tracks}");
        }
        _gaitLibrary = new AnimationLibrary();
        _gaitLibrary.AddAnimation("Run", _gaitClip);
        _player.AddAnimationLibrary("controlledgait", _gaitLibrary);
        GD.Print($"CONTROLLED_GAIT_COMPOSITE candidate={ControlledGaitDrill.Candidate} clip={ControlledGaitDrill.Clip} period={_gaitClip.Length} private={_gaitClip.GetInstanceId()}");
        return "controlledgait/Run";
    }

    public bool Snapshot(EntityView view, long tick, int runToken = 0, int wave = 0)
    {
        if (!Alive() || view.Id != _entity) return false;
        bool reset = tick < _tick || runToken != _runToken || wave != _wave;
        if (reset)
        {
            ResetTransient(); _speed = 0; _runToken = runToken; _wave = wave;
            _previousSample = new Vector3(view.Position.X, 0, view.Position.Y);
        }
        else if (view.Health < _health) _surpriseRemaining = .32f;
        _health = view.Health;
        _target = new Vector3(view.Position.X, 0, view.Position.Y);
        if (tick != _tick)
        {
            _speed = !reset && tick > _tick
                ? _target.DistanceTo(_previousSample) / ((tick - _tick) * GameSimulation.FixedDelta) : 0;
            _previousSample = _target; _tick = tick;
        }
        SetFacing(view.Facing); _root.Visible = view.Active;
        return true;
    }

    /// <summary>Reject stale generation/context and duplicate same-tick cosmetic triggers.</summary>
    public bool Attack(EntityId source, int runToken, int wave, long tick)
    {
        if (!Alive() || source != _entity || runToken != _runToken || wave != _wave || tick < 0 || tick > _tick || tick <= _lastAttackTick)
            return false;
        _lastAttackTick = tick; _attackQueued = true;
        return true;
    }

    internal bool AttackLaunch(ProjectileTransition launch, LaunchAimSample? sample)
    {
        if(launch.Kind!=ProjectileTransitionKind.Launched || !Attack(launch.Owner,launch.RunToken,launch.Wave,launch.Tick)) return false;
        if(sample is not LaunchAimSample aim || aim.Projectile!=launch.Id || aim.Tick!=launch.Tick || !AcceptLaunchAim(aim)) ClearLaunchAim();
        return true;
    }

    private bool _stationaryFacingHeld,_stationaryInputKnown;private float _stationaryFacingYaw;
    internal void SetStationaryFacing(bool held,bool inputKnown,float yaw){_stationaryFacingHeld=held;_stationaryInputKnown=inputKnown;_stationaryFacingYaw=yaw;}

    public void Advance(float delta, bool paused)
    {
        if (!Alive() || paused || delta <= 0 || !float.IsFinite(delta)) return;
        // Defensive presentation clamp only; no simulation time or stats are changed.
        delta = Math.Min(delta, .25f);
        _launchPose?.RestoreBase();
        _movingAim?.RestoreBase();
        _weightTransfer?.RestoreBase();
        _displacementRun?.RestoreBase();
        _turningRun?.RestoreBase();
        _displacementBackpedal?.RestoreBase();
        _displacementSideHop?.RestoreBase();
        Vector3 previousPosition = _root.Position;
        _root.Position = _root.Position.Lerp(_target, 1 - Mathf.Exp(-22 * delta));
        Vector3 actualTravel = _root.Position - previousPosition; actualTravel.Y = 0;
        if (_controlledGait && ControlledGaitDrill.TurningRun)
        {
            bool liftoffFrame = false;
            if (actualTravel.LengthSquared() > .00000001f)
            {
                float nextFacing = Mathf.Atan2(-actualTravel.X, -actualTravel.Z);
                if (_headingBridgeRemaining <= 0 && Math.Abs(Mathf.AngleDifference(_travelFacing, nextFacing)) > Mathf.DegToRad(10))
                {
                    // One actual render step lets the world-anchored support lift before body yaw.
                    // Shared by A/B; it depends only on real travel, never on cosmetic bone feedback.
                    liftoffFrame = true; _headingBridgeRemaining = .1f;
                }
                _travelFacing = nextFacing;
            }
            _headingBridgeRemaining = Math.Max(0, _headingBridgeRemaining - delta);
            // Finish the already-requested cosmetic turn after the position Lerp becomes subpixel.
            // Grounded stop feet target this same final heading before touchdown, never slide afterward.
            if (!liftoffFrame) _root.Rotation = new Vector3(0, Mathf.LerpAngle(_root.Rotation.Y, _travelFacing, 1 - Mathf.Exp(-14 * delta)), 0);
        }
        else if (_controlledGait) _root.Rotation = new Vector3(0, ControlledGaitDrill.Yaw, 0);
        else if(_stationaryFacingHeld){if(_stationaryInputKnown)_root.Rotation=new Vector3(0,StationaryLaunchFacing.Turn(_root.Rotation.Y,_stationaryFacingYaw,delta),0);}
        else _root.Rotation = new Vector3(0, Mathf.LerpAngle(_root.Rotation.Y, _facing, 1 - Mathf.Exp(-14 * delta)), 0);
        bool fireLayer=false;
        if (_attackQueued)
        {
            _attackQueued = false; _smileRemaining = .24f;
            // Coalesce events received while the current gesture is active, including its final step.
            // No delayed gesture backlog; authority hits are never changed here.
            if ((_mixer != null ? !_mixer.AttackActive : _attackRemaining <= 0) && _player != null && _attack.Length > 0)
            {
                _attackRemaining = (float)_player.GetAnimation(_attack).Length;
                if(_mixer!=null) { fireLayer=true; _current=_attack; }
                else Play(_attack, .06, true);
            }
        }
        if (_attackRemaining <= 0)
        {
            string next = _speed > .08f && _run.Length > 0 ? _run : _idle;
            if (next.Length > 0 && next != _current)
            { if(_mixer!=null) _current=next; else Play(next, .12, false); }
        }
        if(_mixer!=null) _mixer.Advance(delta,_speed>.08f,Math.Clamp(_speed/4.8f,.65f,1.6f),fireLayer,false);
        else _player?.Advance(delta);
        if (_weightTransfer != null && _mixer != null) _weightTransfer.Apply(_mixer.RunPhase, _mixer.MovementBlend);
        _displacementRun?.Advance(delta, _speed > .08f, _target);
        _turningRun?.Advance(delta, _speed > .08f, _target);
        _displacementBackpedal?.Advance(delta, _speed > .08f, _target);
        _displacementSideHop?.Advance(delta, _speed > .08f, _target);
        _movingAim?.Advance(delta,_launchAim,_tick,_mixer?.AttackActive ?? _attackRemaining>0);
        _launchPose?.Advance(delta);
        _attackRemaining = Math.Max(0, _attackRemaining - delta);
        // The native graph owns layered gesture lifetime; float countdown must not invent
        // an extra silent clip when an event arrives on the final active render step.
        if(_mixer!=null) _current=_mixer.AttackActive?_attack:(_speed>.08f&&_run.Length>0?_run:_idle);
        _faceClock += delta;
        float cycle = 3.1f + (_entity.Index % 7) * .13f;
        float t = (_faceClock + .5f) % cycle;
        float blink = t < .14f ? Mathf.Sin(Mathf.Pi * t / .14f) : 0;
        float surprise = Mathf.Clamp(_surpriseRemaining / .16f, 0, 1);
        float smile = surprise > 0 ? 0 : Mathf.Clamp(_smileRemaining / .12f, 0, 1) * .45f;
        ApplyFace(blink, smile, surprise);
        _smileRemaining = Math.Max(0, _smileRemaining - delta);
        _surpriseRemaining = Math.Max(0, _surpriseRemaining - delta);
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (GodotObject.IsInstanceValid(_root)) ResetTransient();
        _mixer?.Dispose();
        _disposed = true;
    }

    private bool Alive()
    {
        if (_disposed) return false;
        if (!GodotObject.IsInstanceValid(_root) || _root.IsQueuedForDeletion()) { _disposed = true; return false; }
        return true;
    }
    private void ResetTransient()
    {
        _launchPose?.RestoreBase();
        _movingAim?.RestoreBase();
        _weightTransfer?.RestoreBase();
        _launchPose?.Reset();
        _movingAim?.Reset();
        _displacementRun?.Reset();
        _turningRun?.Reset();
        _displacementBackpedal?.Reset();
        _displacementSideHop?.Reset();
        if (GodotObject.IsInstanceValid(_root)) _travelFacing = _root.Rotation.Y; _headingBridgeRemaining = 0;
        _attackRemaining = _smileRemaining = _surpriseRemaining = _faceClock = 0;
        _attackQueued = false; _lastAttackTick = -1; _current = ""; _launchAim = null;
        if(_mixer!=null) _mixer.Reset();
        else if (_player != null && GodotObject.IsInstanceValid(_player))
        {
            _player.Stop();
            if (_idle.Length > 0) { _player.Play(_idle); _player.Seek(0, true); _player.Advance(0); }
            _player.Stop(true);
        }
        ApplyFace(0, 0, 0);
    }
    private void Play(string clip, double blend, bool restart)
    {
        _player.Play(clip, blend);
        if (restart) _player.Seek(0, true);
        _current = clip;
    }
    private void ApplyFace(float blink, float smile, float surprise)
    {
        foreach (var binding in _face)
            if (GodotObject.IsInstanceValid(binding.Mesh))
                binding.Mesh.SetBlendShapeValue(binding.Index, binding.Channel == 0 ? blink : (binding.Channel == 1 ? smile : surprise) * (binding.HasBlink ? 1 - blink : 1));
    }
    private static void CollectFace(Node node, List<FacialBinding> result)
    {
        if (node is MeshInstance3D mesh && mesh.Mesh != null)
        {
            string[] names = { "Blink", "Smile", "Surprise" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = mesh.FindBlendShapeByName(names[i]);
                if (index >= 0) result.Add(new FacialBinding(mesh, index, i, mesh.FindBlendShapeByName("Blink") >= 0));
            }
        }
        foreach (Node child in node.GetChildren()) CollectFace(child, result);
    }
    private void SetFacing(Vec2 facing)
    { if (facing.LengthSquared > .001f) _facing = Mathf.Atan2(-facing.X, -facing.Y); }
    private string FindClip(string name)
    {
        if (_player == null) return "";
        foreach (string clip in _player.GetAnimationList())
            if (clip.Equals(name, StringComparison.Ordinal) || clip.EndsWith("/" + name, StringComparison.Ordinal)) return clip;
        return "";
    }
    private static AnimationPlayer FindPlayer(Node node)
    {
        if (node is AnimationPlayer player) return player;
        foreach (Node child in node.GetChildren()) { var found = FindPlayer(child); if (found != null) return found; }
        return null;
    }
}
