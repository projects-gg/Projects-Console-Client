using System;
using System.Collections.Generic;

namespace MinecraftClient.Physics
{
    /// <summary>The player's attributes the physics uses.</summary>
    public enum PlayerAttributeKind
    {
        MovementSpeed,
        SneakingSpeed,
    }

    /// <summary>What the game's new player keeps of the last one's attributes (ClientPacketListener.handleRespawn).</summary>
    public enum AttributesKept
    {
        /// <summary>Nothing: a join's player has the defaults until the server sends its own.</summary>
        None,

        /// <summary>The base values (assignBaseValues): a respawn after death; the modifiers go.</summary>
        Bases,

        /// <summary>The base values and the modifiers (assignAllValues): a respawn that keeps them, a change of dimension.</summary>
        All,
    }

    /// <summary>One of the player's own attributes as the server sent it (Update Attributes).</summary>
    public sealed record PlayerAttributeSnapshot(PlayerAttributeKind Kind, double Base,
        IReadOnlyList<PlayerAttribute.Modifier> Modifiers);

    /// <summary>
    /// One of the player's attributes as the game keeps it (AttributeInstance): a base value and modifiers by id. The
    /// server sends the player's own (Update Attributes) and the client takes them as they are, its own modifiers too
    /// (ClientPacketListener.handleUpdateAttributes); the client adds or removes only its own sprint modifier.
    /// </summary>
    public sealed class PlayerAttribute
    {
        /// <summary>A modifier: <c>Operation</c> 0 adds, 1 adds a multiple of the base, 2 multiplies the total.</summary>
        public readonly record struct Modifier(string Id, double Amount, int Operation);

        private readonly double defaultBase, min, max;
        private readonly List<Modifier> modifiers = new();

        public PlayerAttribute(double defaultBase, double min, double max)
        {
            this.defaultBase = defaultBase;
            this.min = min;
            this.max = max;
            BaseValue = defaultBase;
        }

        public double BaseValue { get; private set; }

        /// <summary>The value, as AttributeInstance.calculateValue works it out and RangedAttribute keeps it in range.</summary>
        public double Value
        {
            get
            {
                double value = BaseValue;
                foreach (var modifier in modifiers)
                    if (modifier.Operation == 0)
                        value += modifier.Amount;

                double result = value;
                foreach (var modifier in modifiers)
                    if (modifier.Operation == 1)
                        result += value * modifier.Amount;

                foreach (var modifier in modifiers)
                    if (modifier.Operation == 2)
                        result *= 1.0 + modifier.Amount;

                return double.IsNaN(result) ? min : Math.Clamp(result, min, max);
            }
        }

        /// <summary>What the server sent: its base and its modifiers replace the client's, its own ones too.</summary>
        public void SetFromServer(double baseValue, IEnumerable<Modifier> serverModifiers)
        {
            BaseValue = baseValue;
            modifiers.Clear();
            foreach (var modifier in serverModifiers)
                SetModifier(modifier);
        }

        /// <summary>Add a modifier, in place of one with the same id.</summary>
        public void SetModifier(Modifier modifier)
        {
            RemoveModifier(modifier.Id);
            modifiers.Add(modifier);
        }

        public void RemoveModifier(string id)
        {
            modifiers.RemoveAll(modifier => modifier.Id == id);
        }

        /// <summary>A new player (a join, a respawn) with what it keeps of the last one's attribute.</summary>
        public void ForNewPlayer(AttributesKept kept)
        {
            if (kept != AttributesKept.All)
                modifiers.Clear();
            if (kept == AttributesKept.None)
                BaseValue = defaultBase;
        }
    }
}
