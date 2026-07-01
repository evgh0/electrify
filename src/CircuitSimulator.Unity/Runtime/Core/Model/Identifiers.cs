#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace CircuitSimulator.Core.Model
{
    /// <summary>Identifies a component definition within a circuit.</summary>
    internal readonly struct ComponentId : IEquatable<ComponentId>
    {
        public ComponentId(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Component identifiers cannot be negative.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(ComponentId other) => Value == other.Value;

        public override bool Equals(object? obj) => obj is ComponentId other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => $"ComponentId({Value})";

        public static bool operator ==(ComponentId left, ComponentId right) => left.Equals(right);

        public static bool operator !=(ComponentId left, ComponentId right) => !left.Equals(right);
    }

    /// <summary>Identifies a physical terminal definition within a circuit.</summary>
    internal readonly struct TerminalId : IEquatable<TerminalId>
    {
        public TerminalId(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Terminal identifiers cannot be negative.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(TerminalId other) => Value == other.Value;

        public override bool Equals(object? obj) => obj is TerminalId other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => $"TerminalId({Value})";

        public static bool operator ==(TerminalId left, TerminalId right) => left.Equals(right);

        public static bool operator !=(TerminalId left, TerminalId right) => !left.Equals(right);
    }

    /// <summary>Identifies a compiled electrical node.</summary>
    internal readonly struct NodeId : IEquatable<NodeId>
    {
        public static NodeId Ground { get; } = new NodeId(0);

        public NodeId(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Node identifiers cannot be negative.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(NodeId other) => Value == other.Value;

        public override bool Equals(object? obj) => obj is NodeId other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => $"NodeId({Value})";

        public static bool operator ==(NodeId left, NodeId right) => left.Equals(right);

        public static bool operator !=(NodeId left, NodeId right) => !left.Equals(right);
    }

    /// <summary>Identifies a row or column in an assembled MNA linear system.</summary>
    internal readonly struct VariableIndex : IEquatable<VariableIndex>
    {
        public VariableIndex(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "MNA variable indexes cannot be negative.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(VariableIndex other) => Value == other.Value;

        public override bool Equals(object? obj) => obj is VariableIndex other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => $"VariableIndex({Value})";

        public static bool operator ==(VariableIndex left, VariableIndex right) => left.Equals(right);

        public static bool operator !=(VariableIndex left, VariableIndex right) => !left.Equals(right);
    }

    /// <summary>Identifies an allocated branch-current unknown.</summary>
    internal readonly struct BranchVariableId : IEquatable<BranchVariableId>
    {
        public BranchVariableId(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Branch variable identifiers cannot be negative.");
            }

            Value = value;
        }

        public int Value { get; }

        public bool Equals(BranchVariableId other) => Value == other.Value;

        public override bool Equals(object? obj) => obj is BranchVariableId other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => $"BranchVariableId({Value})";

        public static bool operator ==(BranchVariableId left, BranchVariableId right) => left.Equals(right);

        public static bool operator !=(BranchVariableId left, BranchVariableId right) => !left.Equals(right);
    }
}
