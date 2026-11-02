using System;

namespace Hmi.Core
{
    public readonly struct Vec2
    {
        public readonly float X, Y;
        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }
        public float LengthSquared => X * X + Y * Y;
        public float Length => (float)Math.Sqrt(LengthSquared);
        public Vec2 Normalized => Length > .00001f ? this / Length : default;
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, float b) => new Vec2(a.X * b, a.Y * b);
        public static Vec2 operator /(Vec2 a, float b) => new Vec2(a.X / b, a.Y / b);
        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;
    }

    public readonly struct EntityId : IEquatable<EntityId>
    {
        public readonly int Index, Generation;
        public EntityId(int index, int generation)
        {
            Index = index;
            Generation = generation;
        }
        public bool Equals(EntityId other) => Index == other.Index && Generation == other.Generation;
        public override bool Equals(object? obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => unchecked(Index * 397 ^ Generation);
        public static bool operator ==(EntityId a, EntityId b) => a.Equals(b);
        public static bool operator !=(EntityId a, EntityId b) => !a.Equals(b);
        public static EntityId None => new EntityId(-1, 0);
        public override string ToString() => Index + ":" + Generation;
    }

    public enum RunPhase
    {
        Selecting, Combat, Between, Victory, Defeat, Finished, Prepared
    }
    public enum EntityKind
    {
        Player, Enemy, Companion, Pickup
    }
    public enum CommandKind
    {
        // Value 1 was removed; never reinterpret historical inputs as another command.
        Move = 0, Pause = 2, Choose = 3, Buy = 4, NextWave = 5, CompanionMode = 6, Recall = 7, ContinueEndless = 8, FinishRun = 9
    }
    public readonly struct GameCommand
    {
        public readonly CommandKind Kind;
        public readonly Vec2 Direction;
        public readonly string Id;
        public readonly bool Flag;
        public readonly int RunToken, Wave;
        public GameCommand(CommandKind kind, string id = "", float x = 0, float y = 0,
            bool flag = false, int runToken = 0, int wave = 0)
        {
            Kind = kind;
            Id = id;
            Direction = new Vec2(x, y);
            Flag = flag;
            RunToken = runToken;
            Wave = wave;
        }
        public static GameCommand Move(float x, float y) => new GameCommand(CommandKind.Move, x: x, y: y);
        public static GameCommand Pause(bool value) => new GameCommand(CommandKind.Pause, flag: value);
        public static GameCommand Choose(string id) => new GameCommand(CommandKind.Choose, id);
        public static GameCommand Buy(string id) => new GameCommand(CommandKind.Buy, id);
        public static GameCommand NextWave() => new GameCommand(CommandKind.NextWave);
        public static GameCommand CompanionMode(bool stationary) => new GameCommand(CommandKind.CompanionMode, flag: stationary);
        public static GameCommand Recall() => new GameCommand(CommandKind.Recall);
        public static GameCommand ContinueEndless() => new GameCommand(CommandKind.ContinueEndless);
        public static GameCommand FinishRun() => new GameCommand(CommandKind.FinishRun);
    }
    public readonly struct Wall
    {
        public readonly float X, Y, Width, Height;
        public Wall(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
    public readonly struct EntityView
    {
        public readonly EntityId Id;
        public readonly EntityKind Kind;
        public readonly Vec2 Position, Facing;
        public readonly float Health, MaxHealth;
        public readonly string VisualId;
        public readonly bool Active;
        public EntityView(EntityId id, EntityKind kind, Vec2 position, Vec2 facing,
            float health, float maxHealth, string visualId, bool active = true)
        {
            Id = id;
            Kind = kind;
            Position = position;
            Facing = facing;
            Health = health;
            MaxHealth = maxHealth;
            VisualId = visualId;
            Active = active;
        }
    }
    public enum EventKind
    {
        Melee, Ray, Damage, Death, Impact, Blast, Pickup, WaveEnded
    }
    public readonly struct GameEvent
    {
        public readonly EventKind Kind;
        public readonly FeedbackMechanism Mechanism;
        public readonly EntityId Source, Target;
        public readonly Vec2 From, To;
        public readonly float Value;
        public readonly long Tick;
        public readonly int RunToken, Wave;
        public GameEvent(EventKind kind, EntityId source, EntityId target, Vec2 from, Vec2 to,
            float value, long tick, int runToken, int wave)
            : this(kind, source, target, from, to, value, tick, runToken, wave, FeedbackMechanism.Unknown) { }
        public GameEvent(EventKind kind, EntityId source, EntityId target, Vec2 from, Vec2 to,
            float value, long tick, int runToken, int wave, FeedbackMechanism mechanism)
        {
            Mechanism = mechanism;
            Kind = kind;
            Source = source;
            Target = target;
            From = from;
            To = to;
            Value = value;
            Tick = tick;
            RunToken = runToken;
            Wave = wave;
        }
    }
    public enum Stat
    {
        MaxHealth, BasicRate, BasicRange, BasicDamage, PlayerSpeed, PlayerRiskSpeed,
        EnemySpeed, EnemyHealth, KillReward, OrbitEnabled, OrbitCount, OrbitRadius,
        RicochetEnabled, RicochetBounces, RicochetRange, HoneyEnabled, HoneyRadius,
        HoneySlow, CompanionEnabled, CompanionCount, ImpactEnabled, ImpactRange, ImpactForce, Count
    }

    // Mutable serialization DTOs are copied/validated at the boundary. They are not authoritative state.
    public sealed class GameContent
    {
        public int SchemaVersion { get; set; } = 1;
        public string BalanceVersion { get; set; } = "prototype-1";
        public CharacterDefinition[] Characters { get; set; } = Array.Empty<CharacterDefinition>();
        public CardDefinition[] Cards { get; set; } = Array.Empty<CardDefinition>();
        public ItemDefinition[] Items { get; set; } = Array.Empty<ItemDefinition>();
        public TemporaryContractDefinition[] TemporaryContracts { get; set; } = Array.Empty<TemporaryContractDefinition>();
    }
    public sealed class TemporaryContractDefinition
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Slot { get; set; } = "";
        public string Description { get; set; } = "";
        public ContractEffectDefinition[] Effects { get; set; } = Array.Empty<ContractEffectDefinition>();
    }
    public sealed class ContractEffectDefinition
    {
        public string Stat { get; set; } = "";
        public float PermanentFraction { get; set; }
        public string? RequiredEnabledStat { get; set; }
    }
    public sealed class CharacterDefinition
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Root { get; set; } = "";
        public string VisualId { get; set; } = "";
        public int InitialCompanions { get; set; } = 1;
    }
    public sealed class EffectDefinition
    {
        public string Stat { get; set; } = "";
        public string Operation { get; set; } = "add";
        public float Value
        {
            get; set;
        }
    }
    public enum CardAvailability
    {
        Available, Unknown, Capped, MissingPrerequisite, ExclusiveLocked, RiskLimit
    }
    public sealed class RequirementDefinition
    {
        public string Id { get; set; } = "";
        public int Rank { get; set; } = 1;
    }
    public sealed class CardDefinition
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Slot { get; set; } = "trait";
        public string Effect { get; set; } = "";
        public string? Parent
        {
            get; set;
        }
        public int ParentLevel { get; set; } = 1;
        public RequirementDefinition[]? RequiresAll { get; set; }
        public RequirementDefinition[]? RequiresAny { get; set; }
        public string? ExclusiveGroup { get; set; }
        public int Cap { get; set; } = 1;
        public EffectDefinition[] Effects { get; set; } = Array.Empty<EffectDefinition>();
    }
    public sealed class ItemDefinition
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Effect { get; set; } = "";
        public string Downside { get; set; } = "";
        public int Price
        {
            get; set;
        }
        public int Cap { get; set; } = 1;
        public string Lifetime { get; set; } = "wave";
        public float Duration
        {
            get; set;
        }
        public EffectDefinition[] Effects { get; set; } = Array.Empty<EffectDefinition>();
    }
}
