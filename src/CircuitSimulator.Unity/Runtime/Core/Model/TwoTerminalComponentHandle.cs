#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace CircuitSimulator.Core.Model
{
    /// <summary>Provides stable access to a two-terminal component and its ordered terminals.</summary>
    internal readonly struct TwoTerminalComponentHandle : IEquatable<TwoTerminalComponentHandle>
    {
        public TwoTerminalComponentHandle(
            ComponentId componentId,
            TerminalId positive,
            TerminalId negative)
        {
            ComponentId = componentId;
            Positive = positive;
            Negative = negative;
        }

        public ComponentId ComponentId { get; }

        public TerminalId Positive { get; }

        public TerminalId Negative { get; }

        public bool Equals(TwoTerminalComponentHandle other) =>
            ComponentId == other.ComponentId &&
            Positive == other.Positive &&
            Negative == other.Negative;

        public override bool Equals(object? obj) =>
            obj is TwoTerminalComponentHandle other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = ComponentId.GetHashCode();
                hash = (hash * 397) ^ Positive.GetHashCode();
                hash = (hash * 397) ^ Negative.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(TwoTerminalComponentHandle left, TwoTerminalComponentHandle right) =>
            left.Equals(right);

        public static bool operator !=(TwoTerminalComponentHandle left, TwoTerminalComponentHandle right) =>
            !left.Equals(right);
    }
}
