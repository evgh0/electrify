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
/// Represents the compiled circuit as a node/component multigraph.
/// </summary>
internal sealed class NodeGraph
{
    private readonly Dictionary<NodeId, IReadOnlyList<ComponentId>> _incidentComponents;

    /// <summary>
    /// Initializes a node graph.
    /// </summary>
    /// <param name="nodes">The graph nodes.</param>
    /// <param name="edges">The graph edges.</param>
    public NodeGraph(IEnumerable<ElectricalNode> nodes, IEnumerable<ComponentGraphEdge> edges)
    {
        Guard.NotNull(nodes, nameof(nodes));
        Guard.NotNull(edges, nameof(edges));

        Nodes = new ReadOnlyCollection<ElectricalNode>(nodes.ToArray());
        Edges = new ReadOnlyCollection<ComponentGraphEdge>(edges.ToArray());

        var incidentLists = Nodes.ToDictionary(node => node.Id, static _ => new List<ComponentId>());
        foreach (var edge in Edges)
        {
            foreach (var nodeId in edge.IncidentNodes.Distinct())
            {
                if (!incidentLists.TryGetValue(nodeId, out var incident))
                {
                    throw new ArgumentException($"Edge for component {edge.ComponentId} references unknown node {nodeId}.", nameof(edges));
                }

                incident.Add(edge.ComponentId);
            }
        }

        _incidentComponents = incidentLists.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<ComponentId>)new ReadOnlyCollection<ComponentId>(pair.Value));
    }

    /// <summary>
    /// Gets the graph nodes.
    /// </summary>
    public IReadOnlyList<ElectricalNode> Nodes { get; }

    /// <summary>
    /// Gets graph edges. Parallel components remain distinct edges.
    /// </summary>
    public IReadOnlyList<ComponentGraphEdge> Edges { get; }

    /// <summary>
    /// Gets the components incident to a node.
    /// </summary>
    /// <param name="nodeId">The compiled node identifier.</param>
    /// <returns>The incident component identifiers.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the node does not exist.</exception>
    public IReadOnlyList<ComponentId> GetIncidentComponents(NodeId nodeId) => _incidentComponents[nodeId];

    /// <summary>
    /// Computes connected components of the node multigraph.
    /// </summary>
    /// <returns>The connected node sets ordered by minimum node identifier.</returns>
    public IReadOnlyList<IReadOnlyList<NodeId>> GetConnectedComponents()
    {
        var adjacency = Nodes.ToDictionary(node => node.Id, static _ => new HashSet<NodeId>());

        foreach (var edge in Edges)
        {
            var distinctNodes = edge.IncidentNodes.Distinct().ToArray();
            if (distinctNodes.Length <= 1)
            {
                continue;
            }

            var first = distinctNodes[0];
            for (var index = 1; index < distinctNodes.Length; index++)
            {
                var current = distinctNodes[index];
                adjacency[first].Add(current);
                adjacency[current].Add(first);
            }
        }

        var visited = new HashSet<NodeId>();
        var connectedComponents = new List<IReadOnlyList<NodeId>>();

        foreach (var nodeId in Nodes.Select(node => node.Id).OrderBy(id => id.Value))
        {
            if (!visited.Add(nodeId))
            {
                continue;
            }

            var component = new List<NodeId>();
            var stack = new Stack<NodeId>();
            stack.Push(nodeId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                component.Add(current);

                foreach (var next in adjacency[current].OrderByDescending(id => id.Value))
                {
                    if (visited.Add(next))
                    {
                        stack.Push(next);
                    }
                }
            }

            component.Sort((left, right) => left.Value.CompareTo(right.Value));
            connectedComponents.Add(new ReadOnlyCollection<NodeId>(component));
        }

        connectedComponents.Sort((left, right) => left[0].Value.CompareTo(right[0].Value));
        return new ReadOnlyCollection<IReadOnlyList<NodeId>>(connectedComponents);
    }
}

}
