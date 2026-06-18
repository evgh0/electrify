using System.Collections.ObjectModel;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Compilation;

/// <summary>
/// Represents a physical component after its terminals have been mapped to compiled nodes.
/// </summary>
public sealed class CompiledComponent
{
    /// <summary>
    /// Initializes a compiled component.
    /// </summary>
    /// <param name="componentId">The source component identifier.</param>
    /// <param name="name">The source component name.</param>
    /// <param name="parameters">The immutable component parameters.</param>
    /// <param name="nodes">The compiled nodes in terminal order.</param>
    public CompiledComponent(
        ComponentId componentId,
        string name,
        IComponentParameters parameters,
        IEnumerable<NodeId> nodes)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(nodes);

        ComponentId = componentId;
        Name = name;
        Parameters = parameters;
        Nodes = new ReadOnlyCollection<NodeId>(nodes.ToArray());
    }

    /// <summary>
    /// Gets the source component identifier.
    /// </summary>
    public ComponentId ComponentId { get; }

    /// <summary>
    /// Gets the source component name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the immutable component parameters.
    /// </summary>
    public IComponentParameters Parameters { get; }

    /// <summary>
    /// Gets the component kind.
    /// </summary>
    public ComponentKind Kind => Parameters.Kind;

    /// <summary>
    /// Gets the compiled nodes in terminal order.
    /// </summary>
    public IReadOnlyList<NodeId> Nodes { get; }
}
