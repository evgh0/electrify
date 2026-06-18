using System.Collections.ObjectModel;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Compilation;

/// <summary>
/// Represents one component edge in the compiled node multigraph.
/// </summary>
public sealed class ComponentGraphEdge
{
    /// <summary>
    /// Initializes a graph edge.
    /// </summary>
    /// <param name="componentId">The component represented by the edge.</param>
    /// <param name="incidentNodes">The incident nodes in component terminal order.</param>
    public ComponentGraphEdge(ComponentId componentId, IEnumerable<NodeId> incidentNodes)
    {
        ArgumentNullException.ThrowIfNull(incidentNodes);

        ComponentId = componentId;
        IncidentNodes = new ReadOnlyCollection<NodeId>(incidentNodes.ToArray());
    }

    /// <summary>
    /// Gets the component represented by the edge.
    /// </summary>
    public ComponentId ComponentId { get; }

    /// <summary>
    /// Gets the incident nodes in component terminal order.
    /// </summary>
    public IReadOnlyList<NodeId> IncidentNodes { get; }
}
