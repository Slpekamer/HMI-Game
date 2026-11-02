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
    private readonly EntityId _entity;
    private readonly string _idle, _run, _attack;
    private readonly FacialBinding[] _face;
    private Vector3 _target, _previousSample;
    private long _tick = -1, _lastAttackTick = -1;
    private int _runToken, _wave;
    private float _speed, _attackRemaining, _facing, _health;
    private float _faceClock, _smileRemaining, _surpriseRemaining;
    private string _current = "";
    private bool _attackQueued, _disposed;

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
        SetFacing(view.Facing); _root.Rotation = new Vector3(0, _facing, 0);
        _mixer=FoodAnimationMixer.TryCreate(_player,_idle,_run,_attack);
        ResetTransient();
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

    private bool _stationaryFacingHeld,_stationaryInputKnown;private float _stationaryFacingYaw;
    internal void SetStationaryFacing(bool held,bool inputKnown,float yaw){_stationaryFacingHeld=held;_stationaryInputKnown=inputKnown;_stationaryFacingYaw=yaw;}

    public void Advance(float delta, bool paused)
    {
        if (!Alive() || paused || delta <= 0 || !float.IsFinite(delta)) return;
        // Defensive presentation clamp only; no simulation time or stats are changed.
        delta = Math.Min(delta, .25f);
        _root.Position = _root.Position.Lerp(_target, 1 - Mathf.Exp(-22 * delta));
        if(_stationaryFacingHeld){if(_stationaryInputKnown)_root.Rotation=new Vector3(0,StationaryLaunchFacing.Turn(_root.Rotation.Y,_stationaryFacingYaw,delta),0);}
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
        _attackRemaining = _smileRemaining = _surpriseRemaining = _faceClock = 0;
        _attackQueued = false; _lastAttackTick = -1; _current = "";
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
