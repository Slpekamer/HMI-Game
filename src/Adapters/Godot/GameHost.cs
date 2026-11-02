using Godot;
using Hmi.Core;
using Hmi.GodotAdapter;
using System.Text;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Security.Cryptography;
using Hmi.Adapters.Persistence;

public partial class GameHost : Node3D
{
    private GameContent _content;
    private GameSimulation _sim;
    private GameUi _ui;
    private readonly AssetVisuals _assets = new();
    private Node3D[] _visuals;
    private EntityId[] _ids;
    private CharacterPresentation[] _presentations;
    private GameEvent?[] _pendingAttacks;
    private ReadyAttentionObserver _readyAttention;
    private bool _readyPilot;
    private int _readyRun=-1,_readyWave=-1;private EntityId _readyId=EntityId.None;
    private float _cosmeticYaw;
    // Study diagnostics: desired is not actual; pre-classification and final-draw directions differ by up to one render.
    internal float ReadyDesiredYaw,ReadyAppliedDelta,ReadyHeadingError;
    internal Vector3 ReadyFinalLocalDirection;internal string ReadyReason="Disabled";
    private Camera3D _camera;
    private EnemyHealthVisuals _enemyHealth;
    private SpawnPreviewVisuals _spawnPreviews;
    private EnemyEntrancePresentation _enemyEntrance;
    private MechanicalArrivalVisuals _mechanicalArrival;
    private MeshInstance3D _honey;
    private readonly Node3D[] _orbit = new Node3D[4];
    private readonly MeshInstance3D[] _effects = new MeshInstance3D[64];
    private readonly float[] _effectTime = new float[64];
    private int _effectCursor;
    private double _accumulator, _hudClock;
    private int _frames, _lastWave = -1, _lastCoins = -1, _lastSpent = -1, _heroIndex = 3;
    private uint _seed = 1030;
    private RunPhase _lastPhase = RunPhase.Selecting;
    private bool _lastPaused, _lastPicked, _codexOpen, _codexResume, _smoke;
    private string _capturePath = "";
    private IReadOnlyList<OfferUi> _offers = Array.Empty<OfferUi>();

    public override void _Ready()
    {
        try
        {
            InitializeViewportLayout();
            byte[] catalogBytes = FileAccess.GetFileAsBytes("res://content/gameplay.json");
            _content = JsonSerializer.Deserialize<GameContent>(catalogBytes, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Missing game content");
            string catalogHash = Convert.ToHexString(SHA256.HashData(catalogBytes)).ToLowerInvariant();
            _sim = new GameSimulation(_content, new SimulationLimits(), catalogHash);
            _visuals = new Node3D[_sim.Capacity];
            _ids = new EntityId[_sim.Capacity];
            _presentations = new CharacterPresentation[_sim.Capacity];
            _pendingAttacks = new GameEvent?[_sim.Capacity];
            _enemyEntrance = new EnemyEntrancePresentation(_sim.Capacity);
            _mechanicalArrival = new MechanicalArrivalVisuals(this);
            _readyPilot=OS.GetEnvironment("READY_STRAFE_PILOT")=="1";
            _readyAttention=new ReadyAttentionObserver(_sim.Capacity,OS.GetEnvironment("READY_MELEE_PRIORITY")=="1");
            BuildArena();
            _spawnPreviews = new SpawnPreviewVisuals(this, _sim.Limits.EnemyCapacity);
            var enemyHealthLayer = new CanvasLayer { Name="EnemyHealthLayer", Layer=0 };
            AddChild(enemyHealthLayer);
            _enemyHealth = new EnemyHealthVisuals(_sim.Capacity) { Name="EnemyHealthBars" };
            enemyHealthLayer.AddChild(_enemyHealth);
            InitializePersistence(catalogHash);
            InitializeUi();
            InitializeMovementInput();
            BindPersistenceUi();
            ApplyViewportLayout();
            _ui.ShowHome();
            foreach (string arg in OS.GetCmdlineUserArgs())
            {
                if (arg == "--smoke") _smoke = true;
                if (arg.StartsWith("--capture=", StringComparison.Ordinal)) _capturePath = arg.Substring(10);
            }
            GD.Print("HMI_READY reconstruction=2026-10-31 lighting=linear-direct0.36-ambient0.18");
            if (_smoke) Start(3, 1030);
        }
        catch (Exception e) { GD.PushError(e.ToString()); SetProcess(false); GetTree().Quit(1); }
    }

    private void BuildArena()
    {
        AddChild(new WorldEnvironment { Environment = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("c8b8d9"), AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = Colors.White, AmbientLightEnergy = .18f, TonemapMode = Godot.Environment.ToneMapper.Linear } });
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-55,-25,0), LightEnergy = .36f, ShadowEnabled = true });
        _camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 16.8f, Position = new Vector3(12,22,18), Current = true };
        AddChild(_camera); _camera.LookAt(Vector3.Zero);
        AddChild(AssetVisuals.Box(new Vector3(80,.3f,80), new Vector3(0,-.15f,0), new Color("f4d8ab")));
        Color[] colors = { new("b9a5dd"), new("83b9da"), new("edb3cb"), new("efd18d") };
        for (int x = -14; x <= 14; x++)
        {
            AddChild(AssetVisuals.Box(new Vector3(.95f,.75f,.5f), new Vector3(x,.25f,-8.3f), colors[(x+14)%4]));
            AddChild(AssetVisuals.Box(new Vector3(.95f,.75f,.5f), new Vector3(x,.25f,8.3f), colors[(x+15)%4]));
        }
        for (int y = -7; y <= 7; y++)
        {
            AddChild(AssetVisuals.Box(new Vector3(.5f,.75f,.95f), new Vector3(-14.3f,.25f,y), colors[(y+7)%4]));
            AddChild(AssetVisuals.Box(new Vector3(.5f,.75f,.95f), new Vector3(14.3f,.25f,y), colors[(y+8)%4]));
        }
        foreach (var wall in _sim.Walls)
        {
            // GLB tiles replace the internal-wall placeholder without changing core collision extents.
            const int tiles = 10;
            for (int i = 0; i < tiles; i++)
            {
                var tile = _assets.Create("wall_block");
                tile.Scale = new Vector3(wall.Width / .94883716f, .9f / .8f, wall.Height / tiles / .76279068f);
                tile.Position = new Vector3(wall.X + wall.Width / 2, 0, wall.Y + (i + .5f) * wall.Height / tiles);
                AddChild(tile);
            }
        }
        BuildCandyScenery();
        _honey = AssetVisuals.Disc(new Color(1,.65f,.03f,.25f)); AddChild(_honey); _honey.Visible = false;
        for (int i=0; i<4; i++) { _orbit[i] = _assets.Create("candy_pickup"); _orbit[i].Scale = Vector3.One*.5f; AddChild(_orbit[i]); _orbit[i].Visible = false; }
        for (int i=0; i<_effects.Length; i++) { _effects[i] = AssetVisuals.Box(Vector3.One,Vector3.Zero,new Color("fff5bd")); AddChild(_effects[i]); _effects[i].Visible = false; }
    }


    private void BuildCandyScenery()
    {
        // Presentation-only set dressing; continuous floor and authoritative wall AABBs are unchanged.
        var random = new Random(61031);
        Color[] palette = { new("f7c3d5"), new("b2ddec"), new("d2b9ed"), new("ffe1a1") };
        // Four immutable instance batches replace 220 separate decorative mesh nodes.
        var petalBatches = new MultiMesh[4];
        var petalIndices = new int[4];
        for(int color=0;color<4;color++)
        {
            var material=AssetVisuals.Material(palette[color]);
            material.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;
            var mesh=new CylinderMesh { TopRadius=.07f,BottomRadius=.07f,Height=.01f,RadialSegments=16,Material=material };
            petalBatches[color]=new MultiMesh { TransformFormat=MultiMesh.TransformFormatEnum.Transform3D,Mesh=mesh,InstanceCount=55 };
            AddChild(new MultiMeshInstance3D { Name="CandyPetals_"+color,Multimesh=petalBatches[color],CastShadow=GeometryInstance3D.ShadowCastingSetting.Off });
        }
        for(int i=0;i<44;i++)
        {
            float x=(float)random.NextDouble()*26-13;
            float z=(float)random.NextDouble()*14-7;
            for(int petal=0;petal<5;petal++)
            {
                float angle=petal*Mathf.Tau/5;
                Vector3 position=new(x+Mathf.Cos(angle)*.07f,.006f,z+Mathf.Sin(angle)*.07f);
                petalBatches[i%4].SetInstanceTransform(petalIndices[i%4]++,new Transform3D(Basis.Identity,position));
            }
        }
        for(int side=-1;side<=1;side+=2)
        for(int i=0;i<9;i++)
        {
            float x=-17+i*4;
            float z=side*(12+(i%3)*1.3f);
            float height=1.5f+(i%4)*.6f;
            AddChild(AssetVisuals.Box(new Vector3(2.4f,height,2.4f),new Vector3(x,height/2-.05f,z),palette[i%4]));
            var cap=new MeshInstance3D { Mesh=new SphereMesh { Radius=1.25f,Height=1.4f },Position=new Vector3(x,height,z),MaterialOverride=AssetVisuals.Material(palette[(i+1)%4]) };
            AddChild(cap);
        }
    }

    public override void _Process(double delta)
    {
        if (_sim == null) return;
        PollMobileSafeArea(delta);
        UpdateGamepadUi(delta);
        SynchronizeMovementInput();
        _frames++;
        double elapsed = Math.Min(delta,.25);
        if (_sim.Phase == RunPhase.Combat && !_sim.Paused)
        {
            _accumulator += elapsed;
            int steps = 0;
            while (_accumulator >= GameSimulation.FixedDelta && steps < 8)
            {
                Vector2 move = ReadMoveIntent();
                // Input directions stay screen-relative with the diagonal presentation camera.
                Vector3 right = _camera.GlobalBasis.X; right.Y = 0; right = right.Normalized();
                Vector3 down = _camera.GlobalBasis.Z; down.Y = 0; down = down.Normalized();
                Vector3 worldMove = right * move.X + down * move.Y;
                move = new Vector2(worldMove.X, worldMove.Z).LimitLength();
                _sim.Enqueue(new GameCommand(CommandKind.Move,x:move.X,y:move.Y,runToken:_sim.RunToken,wave:_sim.Wave));
                _sim.Step(); ConsumeEvents(); _accumulator -= GameSimulation.FixedDelta; steps++;
                if (_sim.Paused || _sim.Phase != RunPhase.Combat) { _accumulator = 0; break; }
            }
            if (steps == 8) _accumulator = 0;
        }
        else { _accumulator = 0; _sim.Step(); ConsumeEvents(); }
        RenderEntities((float)elapsed); UpdateEffects(_sim.Paused ? 0 : (float)elapsed);
        _hudClock += elapsed;
        if (_hudClock >= .1 || _lastPhase != _sim.Phase || _lastPaused != _sim.Paused || _lastWave != _sim.Wave || _lastCoins != _sim.Coins || _lastSpent != _sim.Spent || _lastPicked != _sim.CardPicked) { RefreshUi(); _hudClock = 0; }
        if (_smoke && _frames == 120)
        {
            GD.Print($"HMI_SMOKE phase={_sim.Phase} wave={_sim.Wave} entities={_sim.EntityCount} tick={_sim.Tick}");
            if (_capturePath.Length > 0) CallDeferred(nameof(Capture)); else GetTree().Quit();
        }
    }
    private async void Capture()
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var error = GetViewport().GetTexture().GetImage().SavePng(_capturePath);
        GD.Print("HMI_CAPTURE " + error); GetTree().Quit(error == Error.Ok ? 0 : 1);
    }
    private void Start(int hero, uint seed)
    {
        if(!_ruleSelected){RequestRuleStart(hero,seed);return;}
        _seed = seed;
        PerformPersistence(e => _checkpoint.StartNew(AssetVisuals.HeroIds[hero], seed, e), _checkpoint.IntentEpoch);
    }
    private void Send(CommandKind kind, string id = "", bool flag = false)
        => _sim.Enqueue(new GameCommand(kind,id,flag:flag,runToken:_sim.RunToken,wave:_sim.Wave));
    private static Vector3 ToGodot(Vec2 p,float height=0) => new(p.X,height,p.Y);
    public override void _ExitTree()
    {
        ReleaseMovementInput();
        ReleaseViewportLayout();
        if(_visuals!=null) ClearVisuals();
        _spawnPreviews?.Dispose();
        _mechanicalArrival?.Dispose();
        _assets.ReleaseCachedReferences();
    }

    private void ClearVisuals()
    {
        _readyAttention?.Reset();_readyRun=_readyWave=-1;_readyId=EntityId.None;ReadyReason="Reset";
        _cameraBound=false;
        _enemyHealth?.Clear();
        _spawnPreviews?.Clear();
        _enemyEntrance?.Clear();
        _mechanicalArrival?.Clear();
        for(int i=0;i<_visuals.Length;i++) { _presentations[i]?.Dispose(); _visuals[i]?.QueueFree(); _visuals[i]=null; _presentations[i]=null; _pendingAttacks[i]=null; }
        for(int i=0;i<_effects.Length;i++) { _effectTime[i]=0; if(_effects[i]!=null) _effects[i].Visible=false; }
        if(_honey!=null) _honey.Visible=false; foreach(var orbit in _orbit) if(orbit!=null) orbit.Visible=false;
    }
    private void RenderEntities(float delta)
    {
        _spawnPreviews?.Update(_sim);
        _mechanicalArrival?.Update(_sim);
        for(int slot=0;slot<_visuals.Length;slot++)
        {
            if(!_sim.TryGetEntityAtSlot(slot,out var view)) { _presentations[slot]?.Dispose(); _visuals[slot]?.QueueFree(); _visuals[slot]=null; _presentations[slot]=null; _pendingAttacks[slot]=null; continue; }
            if(_visuals[slot]==null || _ids[slot]!=view.Id)
            {
                _presentations[slot]?.Dispose();
                _visuals[slot]?.QueueFree();
                // A reused slot may change kind (actor -> pickup). Always discard the old
                // presenter before binding the new generation, including non-animated kinds.
                _presentations[slot]=null;
                if(_pendingAttacks[slot]?.Source!=view.Id) _pendingAttacks[slot]=null;
                _visuals[slot]=_assets.Create(view.Kind==EntityKind.Pickup?"candy_pickup":view.VisualId);
                _ids[slot]=view.Id; AddChild(_visuals[slot]);
                if(view.Kind!=EntityKind.Pickup)
                {
                    _visuals[slot].Scale=Vector3.One*(view.Kind==EntityKind.Player?1.35f:1.15f);
                    _presentations[slot]=new CharacterPresentation(_visuals[slot],view,_sim.Tick,_sim.RunToken,_sim.Wave,_readyPilot&&view.Kind==EntityKind.Player&&_heroIndex==1);
                }
            }
            var node=_visuals[slot];
            var presentation=_presentations[slot];
            if(presentation!=null)
            {
                presentation.Snapshot(view,_sim.Tick,_sim.RunToken,_sim.Wave);
                if(_pendingAttacks[slot] is GameEvent attack)
                {
                    presentation.Attack(attack);
                    _pendingAttacks[slot]=null;
                }
                if(_readyPilot&&view.Kind==EntityKind.Player&&_heroIndex==1)
                    presentation.ClassifyLocomotion(_readyAttention.OutputValid&&(_readyAttention.Desired.AttentionTarget!=null||_readyAttention.Desired.Reason==Hmi.PresentationStudy.HeadingReason.ActualMelee||_readyAttention.Desired.Reason==Hmi.PresentationStudy.HeadingReason.AuthoritativeMeleeAim),_sim.Paused||_sim.Phase!=RunPhase.Combat);
                presentation.Advance(delta,_sim.Paused || _sim.Phase!=RunPhase.Combat);
            }
            else { node.Visible=view.Active; node.Position=ToGodot(view.Position); }
            if(view.Kind==EntityKind.Enemy) node.Scale=new Vector3(1.15f,1.15f*_enemyEntrance.VerticalScale(view.Id,_sim.RunToken,_sim.Wave,_sim.Tick),1.15f);
            if(view.Kind==EntityKind.Pickup) { node.Position+=new Vector3(0,.18f+Mathf.Sin(_sim.WaveSeconds*3)*.06f,0); node.Rotation=new Vector3(0,_sim.WaveSeconds,0); }
        }
        bool combat=_sim.Phase==RunPhase.Combat;
        if(_sim.TryGetEntity(_sim.PlayerId,out var player))
        {
            UpdateCameraFollow(player,delta);
            _honey.Visible=combat&&_sim.GetStat(Stat.HoneyEnabled)>0; _honey.Position=ToGodot(player.Position,.015f);
            float radius=_sim.GetStat(Stat.HoneyRadius); _honey.Scale=new Vector3(radius,1,radius);
            int count=(int)_sim.GetStat(Stat.OrbitCount); float orbitRadius=_sim.GetStat(Stat.OrbitRadius);
            for(int i=0;i<4;i++)
            {
                _orbit[i].Visible=combat&&_sim.GetStat(Stat.OrbitEnabled)>0&&i<count;
                float angle=_sim.WaveSeconds*2.1f+i*Mathf.Tau/Math.Max(1,count);
                _orbit[i].Position=ToGodot(player.Position+new Vec2(Mathf.Cos(angle),Mathf.Sin(angle))*orbitRadius,.6f);
            }
        }
        // Project after the camera and interpolated actors have moved; one draw list, never per-enemy UI nodes.
        _enemyHealth.BeginFrame();
        for(int slot=0;slot<_visuals.Length;slot++)
            if(_visuals[slot]!=null && _sim.TryGetEntityAtSlot(slot,out var healthView))
                _enemyHealth.Update(slot,healthView,_visuals[slot],_camera,combat);
        _enemyHealth.EndFrame();
        UpdateReadyFacing(delta);
    }
    private void UpdateReadyFacing(float delta)
    {
        ReadyAppliedDelta=0;
        if(!_readyPilot||_heroIndex!=1||_sim.Phase!=RunPhase.Combat||!_sim.TryGetEntity(_sim.PlayerId,out var p))
        {_readyAttention.Reset();ReadyReason="Disabled";return;}
        var actor=_visuals[p.Id.Index];var presenter=_presentations[p.Id.Index];
        if(actor==null||presenter==null){_readyAttention.Reset();ReadyReason="NoActor";return;}
        if(_readyRun!=_sim.RunToken||_readyWave!=_sim.Wave||_readyId!=p.Id)
        {
            _readyAttention.ResetObservation();_readyRun=_sim.RunToken;_readyWave=_sim.Wave;_readyId=p.Id;
            _cosmeticYaw=p.Facing.LengthSquared>.000001f?Mathf.Atan2(-p.Facing.X,-p.Facing.Y):actor.Rotation.Y;
            actor.Rotation=new Vector3(0,_cosmeticYaw,0);
        }
        if(!_readyAttention.Observe(_sim,_camera,_visuals,_ids,presenter,delta))
        {ReadyReason="InvalidObservation";return;}
        var heading=_readyAttention.Desired;ReadyReason=heading.Reason.ToString();
        ReadyDesiredYaw=Mathf.Atan2(-heading.Direction.X,-heading.Direction.Y);
        float error=Mathf.Wrap(ReadyDesiredYaw-_cosmeticYaw,-Mathf.Pi,Mathf.Pi);
        if(!_sim.Paused&&float.IsFinite(delta)&&delta>0)
        {
            float limit=Mathf.DegToRad(240)*Math.Min(delta,.25f);ReadyAppliedDelta=Math.Clamp(error,-limit,limit);
            _cosmeticYaw=Mathf.Wrap(_cosmeticYaw+ReadyAppliedDelta,-Mathf.Pi,Mathf.Pi);
            actor.Rotation=new Vector3(0,_cosmeticYaw,0);
        }
        ReadyHeadingError=Math.Abs(Mathf.Wrap(ReadyDesiredYaw-_cosmeticYaw,-Mathf.Pi,Mathf.Pi));
        var motion=presenter.ActualMotion;motion.Y=0;ReadyFinalLocalDirection=motion.LengthSquared()>.000001f?actor.GlobalBasis.Orthonormalized().Inverse()*motion.Normalized():Vector3.Zero;
    }
    // Preserve authoritative context across catch-up ticks; coalesce to the newest cosmetic gesture.
    private bool QueuePresentationEvent(GameEvent e)
    {
        if(e.RunToken!=_sim.RunToken || e.Wave!=_sim.Wave || e.Tick<0 || e.Tick>_sim.Tick ||
           e.Source.Index<0 || e.Source.Index>=_pendingAttacks.Length || !_sim.TryGetEntity(e.Source,out _)) return false;
        if(e.Kind!=EventKind.Melee && e.Kind!=EventKind.Ray && e.Kind!=EventKind.Impact &&
           !(e.Kind==EventKind.Damage && e.Target==_sim.PlayerId)) return false;
        var previous=_pendingAttacks[e.Source.Index];
        if(previous is GameEvent old && old.Source==e.Source && old.RunToken==e.RunToken && old.Wave==e.Wave && old.Tick>=e.Tick) return false;
        _pendingAttacks[e.Source.Index]=e;
        return true;
    }
    private void ConsumeEvents()
    {
        if(_readyPilot&&_heroIndex==1)_readyAttention?.CaptureCoreStep(_sim);
        _enemyEntrance?.Observe(_sim);
        _mechanicalArrival?.Observe(_sim);
        for(int i=0;i<_sim.EventCount;i++)
        {
            var e=_sim.GetEvent(i);
            if(e.RunToken!=_sim.RunToken || e.Wave!=_sim.Wave) continue;
            QueuePresentationEvent(e);
            if(e.Kind!=EventKind.Ray && e.Kind!=EventKind.Melee && e.Kind!=EventKind.Impact && e.Kind!=EventKind.Blast && !(e.Kind==EventKind.Damage&&e.Target==_sim.PlayerId)) continue;
            int slot=_effectCursor++%_effects.Length;
            var mesh=_effects[slot]; var a=ToGodot(e.From,.55f); var b=ToGodot(e.To,.55f); float length=a.DistanceTo(b);
            mesh.Position=(a+b)*.5f; mesh.Scale=new Vector3(.07f,.07f,Math.Max(.12f,length));
            if(length>.001f) mesh.LookAt(b);
            mesh.Visible=true; _effectTime[slot]=.16f;
        }
    }
    private void UpdateEffects(float delta)
    {
        for(int i=0;i<_effects.Length;i++) if(_effectTime[i]>0) { _effectTime[i]-=delta; if(_effectTime[i]<=0) _effects[i].Visible=false; }
    }
    private void InitializeUi()
    {
        _ui=new GameUi(); AddChild(_ui);
        _ui.StartRequested=RequestStart;
        _ui.PauseRequested=()=> { if(!_codexOpen && !_ui.IsPersistenceModalOpen) Send(CommandKind.Pause,flag:!_sim.Paused); };
        _ui.StationRequested=()=>Send(CommandKind.CompanionMode,flag:!_sim.CompanionsStationary);
        _ui.RecallRequested=()=>Send(CommandKind.Recall);
        _ui.RestartRequested=Selection;
        _ui.CodexRequested=OpenCodex;
        _ui.CodexClosed=CloseCodex;
    }
    private void Selection()
    {
        if(_ui.IsPersistenceModalOpen || _checkpoint.HasPendingFailure) return;
        if(_checkpoint.CurrentRecovery.Kind==RecoveryKind.Closed) { ShowSelectionAfterPersistence(); return; }
        PerformPersistence(e => _checkpoint.Close(e), _checkpoint.IntentEpoch, toSelection:true);
    }
    public override void _Input(InputEvent input)
    {
        if(HandleGamepadEvent(input))return;
        if(_sim==null || input is not InputEventKey key || !key.Pressed || key.Echo) return;
        if(key.Keycode==Key.Escape)
        {
            if(_ui.IsPersistenceModalOpen) _ui.TryCancelPersistenceModal();
            else if(_ui.IsCodexOpen) _ui.CloseCodex();
            else if(_sim.Phase==RunPhase.Combat) Send(CommandKind.Pause,flag:!_sim.Paused);
            GetViewport().SetInputAsHandled();
        }
    }
    public override void _Notification(int what)
    {
        if(what==NotificationApplicationFocusIn){_inputFocused=true;ResetMovementInput();PollMobileSafeArea(0,true);}
        if(what!=NotificationApplicationFocusOut || _sim==null) return;
        _inputFocused=false;ResetMovementInput();_readyAttention?.SuspendAim();
        _codexResume=false; _accumulator=0; _ui?.Joystick.ResetInput();
        if(_sim.Phase==RunPhase.Combat) Send(CommandKind.Pause,flag:true);
    }
    private string OpenCodex()
    {
        _codexOpen=true; _codexResume=_sim.Phase==RunPhase.Combat && !_sim.Paused;
        if(_sim.Phase==RunPhase.Combat) Send(CommandKind.Pause,flag:true);
        _ui.CancelGameplayInput(); _accumulator=0;
        return CodexFormatter.Build(_content, _sim.GetLevel, _sim.GetCardAvailability);
    }
    private void CloseCodex()
    {
        _codexOpen=false;
        if(_codexResume && !_ui.IsPersistenceModalOpen && _sim.Phase==RunPhase.Combat) Send(CommandKind.Pause,flag:false);
        else if(_sim.Phase==RunPhase.Combat) _ui.ShowPause(true);
        _codexResume=false;
    }
    private void RefreshUi()
    {
        bool phaseChanged=_lastPhase!=_sim.Phase || _lastWave!=_sim.Wave;
        if(_sim.Phase==RunPhase.Combat)
        {
            _ui.ShowCombat();
            if(_sim.TryGetEntity(_sim.PlayerId,out var player))
                _ui.UpdateHud(AssetVisuals.HeroNames[_heroIndex],player.Health,player.MaxHealth,_sim.Wave,WaveRules.DurationSeconds-_sim.WaveSeconds,_sim.Coins,
                    (ContractStatus(false).Length>0?ContractStatus(false)+" · ":"")+(_sim.PowerSeconds>0?$"强攻 {_sim.PowerSeconds:0}s  ":"")+(_sim.HasteSeconds>0?$"轻快 {_sim.HasteSeconds:0}s":""),_sim.GetStat(Stat.CompanionEnabled)<=0?"可通过特性卡获得独立伙伴":_sim.CompanionsStationary?"伙伴驻守当前位置":"伙伴独立游走 · 不跟随主角",_sim.GetStat(Stat.CompanionEnabled)>0,_sim.IsEndless,_sim.EndlessWavesCompleted);
            if(!_codexOpen && (phaseChanged || _lastPaused!=_sim.Paused)) _ui.ShowPause(_sim.Paused);
        }
        else if(_sim.Phase==RunPhase.Between && (phaseChanged || _lastCoins!=_sim.Coins || _lastSpent!=_sim.Spent || _lastPicked!=_sim.CardPicked))
        {
            if(!_sim.CardPicked)
            {
                var offers=new List<OfferUi>();
                foreach(var slot in _sim.GetOfferSlots()) offers.Add(EndlessUiFormatter.Offer(slot));
                _offers=offers;
            }
            int token=_sim.RunToken,wave=_sim.Wave;
            _ui.PickRequested=id=>_sim.Enqueue(new GameCommand(CommandKind.Choose,id,runToken:token,wave:wave));
            _ui.BuyRequested=id=>_sim.Enqueue(new GameCommand(CommandKind.Buy,id,runToken:token,wave:wave));
            long checkpointEpoch=_checkpoint.IntentEpoch;
            _ui.NextRequested=()=> { if(_sim.RunToken==token && _sim.Wave==wave && !_ui.IsPersistenceModalOpen)
                PerformPersistence(e=>_checkpoint.PrepareNext(e),checkpointEpoch); };
            _ui.FinishRequested=()=>_sim.Enqueue(new GameCommand(CommandKind.FinishRun,runToken:token,wave:wave));
            var items=new List<OfferUi>();
            foreach(var item in _content.Items) items.Add(new OfferUi(item.Id,item.Name,item.Effect+"\n"+(_sim.GetItemStock(item.Id)==0?"本轮已购":item.Downside),$"{item.Price} 金币",_sim.CanBuy(item.Id)));
            _ui.ShowBetween(_offers,_sim.Coins,_sim.Earned,_sim.Spent,_sim.CardPicked,items,_ui.IsShop||_sim.CardPicked,ContractStatus(true),_sim.IsEndless);
        }
        else if(phaseChanged && (_sim.Phase==RunPhase.Victory || _sim.Phase==RunPhase.Defeat || _sim.Phase==RunPhase.Finished))
        {
            int token=_sim.RunToken,wave=_sim.Wave;
            _ui.ContinueRequested=()=>_sim.Enqueue(new GameCommand(CommandKind.ContinueEndless,runToken:token,wave:wave));
            _ui.FinishRequested=()=>_sim.Enqueue(new GameCommand(CommandKind.FinishRun,runToken:token,wave:wave));
            _ui.ShowRunEnding(_sim.Phase==RunPhase.Victory,_sim.Phase==RunPhase.Defeat,_sim.FiniteCompleted,_sim.EndlessWavesCompleted,_sim.Kills,_sim.Coins,_sim.Earned,_sim.Spent,EndlessUiFormatter.Termination(_sim.TerminationReason));
            if((_sim.Phase==RunPhase.Defeat || _sim.Phase==RunPhase.Finished) && _terminalCloseRun!=_sim.RunToken)
            {
                _terminalCloseRun=_sim.RunToken;
                PerformPersistence(e=>_checkpoint.Close(e),_checkpoint.IntentEpoch);
            }
        }
        _lastPhase=_sim.Phase; _lastWave=_sim.Wave; _lastCoins=_sim.Coins; _lastSpent=_sim.Spent; _lastPicked=_sim.CardPicked; _lastPaused=_sim.Paused;
    }
    private string ContractStatus(bool pending)
    {
        string id=pending?_sim.PendingContractId:_sim.ActiveContractId;
        if(id!=null)foreach(var contract in _content.TemporaryContracts)
            if(contract.Id==id)return (pending?"下一波：":"生效中：")+contract.Name;
        if(pending&&_sim.PendingNextWaveSupply)return "下一波：临时护体补给";
        if(!pending&&_sim.ActiveSupplyHealth>0)return "生效中：临时护体补给";
        return "";
    }

}
