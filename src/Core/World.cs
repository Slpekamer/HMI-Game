using System;
using System.Collections.Generic;

namespace Hmi.Core
{
    [Flags]
    internal enum Components
    {
        None = 0, Position = 1, Health = 2, Player = 4, Enemy = 8, Companion = 16, Pickup = 32
    }
    internal struct TransformComponent
    {
        public Vec2 Position, Facing; public float Radius;
    }
    internal struct HealthComponent
    {
        public float Current, Maximum;
    }
    internal struct EnemyComponent
    {
        public float AttackCooldown, OrbitCooldown, KnockTime, WallCooldown;
        public Vec2 KnockVelocity;
        public int Bounces;
        public bool BlastUsed;
    }
    internal struct CompanionComponent
    {
        public Vec2 Anchor; public float Cooldown, BuffTime; public bool Support; public int SpawnOrdinal;
    }
    internal struct PickupComponent
    {
        public int Type; public float Cooldown;
    }
    internal sealed class World
    {
        internal readonly Components[] Mask;
        internal readonly int[] Generation;
        internal readonly TransformComponent[] Transform;
        internal readonly HealthComponent[] Health;
        internal readonly EnemyComponent[] Enemy;
        internal readonly CompanionComponent[] Companion;
        internal readonly PickupComponent[] Pickup;
        internal readonly string[] Visual;
        internal readonly bool[] PendingDeath;
        internal int Count;
        internal int Capacity => Mask.Length;
        internal World(int capacity)
        {
            Mask = new Components[capacity];
            Generation = new int[capacity];
            Transform = new TransformComponent[capacity];
            Health = new HealthComponent[capacity];
            Enemy = new EnemyComponent[capacity];
            Companion = new CompanionComponent[capacity];
            Pickup = new PickupComponent[capacity];
            Visual = new string[capacity];
            PendingDeath = new bool[capacity];
        }
        internal EntityId Create(Components mask, Vec2 position, float radius, string visual, float hp = 0)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (Mask[i] != Components.None || Generation[i] < 0 || Generation[i] == int.MaxValue)
                    continue;
                Generation[i]++;
                Mask[i] = mask | Components.Position;
                Transform[i] = new TransformComponent { Position = position, Radius = radius, Facing = new Vec2(0, 1) };
                Health[i] = new HealthComponent { Current = hp, Maximum = hp };
                Enemy[i] = default;
                Companion[i] = default;
                Pickup[i] = default;
                Visual[i] = visual;
                PendingDeath[i] = false;
                Count++;
                return Id(i);
            }
            return EntityId.None;
        }
        internal bool HasFreeSlot
        {
            get
            {
                for (int i = 0; i < Capacity; i++)
                    if (Mask[i] == Components.None && Generation[i] >= 0 && Generation[i] < int.MaxValue) return true;
                return false;
            }
        }
        internal bool Alive(EntityId id) => id.Index >= 0 && id.Index < Capacity
            && Generation[id.Index] == id.Generation && Mask[id.Index] != Components.None && !PendingDeath[id.Index];
        internal bool Has(int i, Components mask) => (Mask[i] & mask) == mask && !PendingDeath[i];
        internal EntityId Id(int i) => new EntityId(i, Generation[i]);
        internal void Destroy(int i)
        {
            if (Mask[i] == Components.None)
                return;
            Mask[i] = Components.None;
            PendingDeath[i] = false;
            Visual[i] = "";
            Count--;
        }
        internal void Clear()
        {
            for (int i = 0; i < Capacity; i++)
                Destroy(i);
        }
    }
    internal interface ISimulationSystem
    {
        void Update(GameSimulation game);
    }
    internal readonly struct CompiledEffect
    {
        internal readonly Stat Stat;
        internal readonly bool Multiply;
        internal readonly float Value;
        internal CompiledEffect(Stat stat, bool multiply, float value)
        {
            Stat = stat;
            Multiply = multiply;
            Value = value;
        }
    }
    internal sealed class RunProgressionResource
    {
        internal RunPhase Phase = RunPhase.Selecting;
        internal long Tick, PreparationOrdinal;
        internal int RunToken, Wave, CharacterIndex;
        internal float WaveSeconds;
        internal bool Paused, FiniteCompleted, IsEndless, PendingSupply;
        internal int EndlessWavesCompleted;
        internal float SupplyHealthBonus;
        internal uint EndlessOfferHistory;
        internal RunTerminationReason TerminationReason;
        internal int PendingContract = -1, ActiveContract = -1;
        internal uint Seed, Random;
        internal EntityId PlayerId = EntityId.None;
    }
    internal sealed class WalletResource
    {
        internal int Coins, Earned, Spent, Kills;
        internal readonly int[] OwnedItems, ActiveItems, Stock;
        internal WalletResource(int count)
        {
            OwnedItems = new int[count];
            ActiveItems = new int[count];
            Stock = new int[count];
        }
    }
    internal sealed class CardLoadoutResource
    {
        internal readonly int[] Levels;
        internal readonly int[] Offers = new int[3];
        internal readonly List<string> Picks = new List<string>(5);
        internal IReadOnlyList<CardDefinition> OfferViews = Array.Empty<CardDefinition>();
        internal IReadOnlyList<OfferSlotView> OfferSlots = Array.Empty<OfferSlotView>();
        internal bool CardPicked;
        internal CardLoadoutResource(int count)
        {
            Levels = new int[count];
        }
    }
}
