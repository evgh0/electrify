#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections.ObjectModel;

namespace CircuitSimulator.Core.Model
{

/// <summary>
/// Immutable physical circuit definition containing components, terminals, ideal wires, and ground markers.
/// </summary>
internal sealed class Circuit
{
    private readonly Dictionary<ComponentId, ComponentDefinition> _componentsById;
    private readonly Dictionary<TerminalId, TerminalDefinition> _terminalsById;

    /// <summary>
    /// Initializes a new immutable circuit snapshot.
    /// </summary>
    /// <param name="components">The component definitions.</param>
    /// <param name="terminals">The terminal definitions.</param>
    /// <param name="wires">The ideal wire definitions.</param>
    /// <param name="groundTerminals">The terminals marked as ground.</param>
    public Circuit(
        IEnumerable<ComponentDefinition> components,
        IEnumerable<TerminalDefinition> terminals,
        IEnumerable<WireDefinition> wires,
        IEnumerable<TerminalId> groundTerminals)
    {
        Guard.NotNull(components, nameof(components));
        Guard.NotNull(terminals, nameof(terminals));
        Guard.NotNull(wires, nameof(wires));
        Guard.NotNull(groundTerminals, nameof(groundTerminals));

        var componentArray = components.ToArray();
        var terminalArray = terminals.ToArray();
        var wireArray = wires.ToArray();
        var groundArray = groundTerminals.Distinct().OrderBy(id => id.Value).ToArray();

        if (componentArray.Any(static component => component is null))
        {
            throw new ArgumentException("Component collections cannot contain null entries.", nameof(components));
        }

        if (terminalArray.Any(static terminal => terminal is null))
        {
            throw new ArgumentException("Terminal collections cannot contain null entries.", nameof(terminals));
        }

        if (wireArray.Any(static wire => wire is null))
        {
            throw new ArgumentException("Wire collections cannot contain null entries.", nameof(wires));
        }

        _componentsById = componentArray.ToDictionary(component => component.Id);
        _terminalsById = terminalArray.ToDictionary(terminal => terminal.Id);

        Components = new ReadOnlyCollection<ComponentDefinition>(componentArray);
        Terminals = new ReadOnlyCollection<TerminalDefinition>(terminalArray);
        Wires = new ReadOnlyCollection<WireDefinition>(wireArray);
        GroundTerminals = new ReadOnlyCollection<TerminalId>(groundArray);
    }

    /// <summary>
    /// Gets the immutable component definitions.
    /// </summary>
    public IReadOnlyList<ComponentDefinition> Components { get; }

    /// <summary>
    /// Gets the immutable terminal definitions.
    /// </summary>
    public IReadOnlyList<TerminalDefinition> Terminals { get; }

    /// <summary>
    /// Gets the immutable ideal wire definitions.
    /// </summary>
    public IReadOnlyList<WireDefinition> Wires { get; }

    /// <summary>
    /// Gets the terminal identifiers marked as ground.
    /// </summary>
    public IReadOnlyCollection<TerminalId> GroundTerminals { get; }

    /// <summary>
    /// Gets a component definition by identifier.
    /// </summary>
    /// <param name="componentId">The component identifier.</param>
    /// <returns>The matching component definition.</returns>
    /// <exception cref="CircuitModelException">Thrown when the component does not exist.</exception>
    public ComponentDefinition GetComponent(ComponentId componentId)
    {
        if (_componentsById.TryGetValue(componentId, out var component))
        {
            return component;
        }

        throw new CircuitModelException($"Component {componentId} does not exist in the circuit.");
    }

    /// <summary>
    /// Gets a terminal definition by identifier.
    /// </summary>
    /// <param name="terminalId">The terminal identifier.</param>
    /// <returns>The matching terminal definition.</returns>
    /// <exception cref="CircuitModelException">Thrown when the terminal does not exist.</exception>
    public TerminalDefinition GetTerminal(TerminalId terminalId)
    {
        if (_terminalsById.TryGetValue(terminalId, out var terminal))
        {
            return terminal;
        }

        throw new CircuitModelException($"Terminal {terminalId} does not exist in the circuit.");
    }

    /// <summary>
    /// Determines whether a terminal exists in the circuit.
    /// </summary>
    /// <param name="terminalId">The terminal identifier.</param>
    /// <returns><see langword="true"/> when the terminal exists; otherwise <see langword="false"/>.</returns>
    public bool ContainsTerminal(TerminalId terminalId) => _terminalsById.ContainsKey(terminalId);
}

}
