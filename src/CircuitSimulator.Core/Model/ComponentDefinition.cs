using System.Collections.ObjectModel;
using CircuitSimulator.Core.Components;

namespace CircuitSimulator.Core.Model;

/// <summary>
/// Defines an immutable physical component and its terminal ownership.
/// </summary>
public sealed class ComponentDefinition
{
    private readonly ReadOnlyCollection<TerminalId> _terminalIds;

    /// <summary>
    /// Initializes a new component definition.
    /// </summary>
    /// <param name="id">The component identifier.</param>
    /// <param name="name">The component name.</param>
    /// <param name="terminalIds">The terminal identifiers in local terminal order.</param>
    /// <param name="parameters">The immutable component parameters.</param>
    public ComponentDefinition(
        ComponentId id,
        string name,
        IEnumerable<TerminalId> terminalIds,
        IComponentParameters parameters)
    {
        Guard.NotNull(name, nameof(name));
        Guard.NotNull(terminalIds, nameof(terminalIds));
        Guard.NotNull(parameters, nameof(parameters));

        Id = id;
        Name = name;
        _terminalIds = new ReadOnlyCollection<TerminalId>(terminalIds.ToArray());
        Parameters = parameters;
    }

    /// <summary>
    /// Gets the component identifier.
    /// </summary>
    public ComponentId Id { get; }

    /// <summary>
    /// Gets the component name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the terminal identifiers in local terminal order.
    /// </summary>
    public IReadOnlyList<TerminalId> TerminalIds => _terminalIds;

    /// <summary>
    /// Gets the immutable component parameters.
    /// </summary>
    public IComponentParameters Parameters { get; }

    /// <summary>
    /// Gets the component kind.
    /// </summary>
    public ComponentKind Kind => Parameters.Kind;
}
