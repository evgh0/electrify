using System.Collections.ObjectModel;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Compilation;

/// <summary>
/// Immutable compiled terminal-to-node topology.
/// </summary>
public sealed class Netlist
{
    private readonly IReadOnlyDictionary<TerminalId, NodeId> _terminalToNode;

    /// <summary>
    /// Initializes a new netlist.
    /// </summary>
    /// <param name="nodes">The compiled electrical nodes.</param>
    /// <param name="terminalToNode">The terminal-to-node mapping.</param>
    /// <param name="groundNodeId">The deterministic ground node identifier.</param>
    public Netlist(
        IEnumerable<ElectricalNode> nodes,
        IReadOnlyDictionary<TerminalId, NodeId> terminalToNode,
        NodeId groundNodeId)
    {
        Guard.NotNull(nodes, nameof(nodes));
        Guard.NotNull(terminalToNode, nameof(terminalToNode));

        Nodes = new ReadOnlyCollection<ElectricalNode>(nodes.ToArray());
        _terminalToNode = new ReadOnlyDictionary<TerminalId, NodeId>(new Dictionary<TerminalId, NodeId>(terminalToNode));
        GroundNodeId = groundNodeId;
    }

    /// <summary>
    /// Gets the compiled electrical nodes.
    /// </summary>
    public IReadOnlyList<ElectricalNode> Nodes { get; }

    /// <summary>
    /// Gets the terminal-to-node mapping.
    /// </summary>
    public IReadOnlyDictionary<TerminalId, NodeId> TerminalToNode => _terminalToNode;

    /// <summary>
    /// Gets the deterministic ground node identifier.
    /// </summary>
    public NodeId GroundNodeId { get; }

    /// <summary>
    /// Gets the compiled node for a physical terminal.
    /// </summary>
    /// <param name="terminalId">The terminal identifier.</param>
    /// <returns>The compiled node identifier.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the terminal is not part of the netlist.</exception>
    public NodeId GetNode(TerminalId terminalId) => _terminalToNode[terminalId];
}
