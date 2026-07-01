#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections.ObjectModel;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Compilation
{

/// <summary>
/// Represents one compiled electrical node formed by ideal-wire-connected terminals.
/// </summary>
internal sealed class ElectricalNode
{
    /// <summary>
    /// Initializes a new electrical node.
    /// </summary>
    /// <param name="id">The node identifier.</param>
    /// <param name="isGround">Whether this node is the ground reference.</param>
    /// <param name="terminalIds">The physical terminals belonging to this node.</param>
    public ElectricalNode(NodeId id, bool isGround, IEnumerable<TerminalId> terminalIds)
    {
        Guard.NotNull(terminalIds, nameof(terminalIds));

        Id = id;
        IsGround = isGround;
        TerminalIds = new ReadOnlyCollection<TerminalId>(terminalIds.OrderBy(terminal => terminal.Value).ToArray());
    }

    /// <summary>
    /// Gets the node identifier.
    /// </summary>
    public NodeId Id { get; }

    /// <summary>
    /// Gets a value indicating whether this node is the ground reference node.
    /// </summary>
    public bool IsGround { get; }

    /// <summary>
    /// Gets the physical terminals in this node.
    /// </summary>
    public IReadOnlyList<TerminalId> TerminalIds { get; }
}

}
