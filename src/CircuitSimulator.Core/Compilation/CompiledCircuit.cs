using System.Collections.ObjectModel;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Validation;

namespace CircuitSimulator.Core.Compilation;

/// <summary>
/// Immutable compiled circuit containing topology, graph, validation warnings, and MNA variable allocation.
/// </summary>
public sealed class CompiledCircuit
{
    private readonly Dictionary<ComponentId, CompiledComponent> _componentsById;

    /// <summary>
    /// Initializes a compiled circuit.
    /// </summary>
    /// <param name="sourceCircuit">The source physical circuit snapshot.</param>
    /// <param name="netlist">The compiled terminal-to-node netlist.</param>
    /// <param name="graph">The compiled node graph.</param>
    /// <param name="components">The compiled components.</param>
    /// <param name="variableMap">The MNA variable map.</param>
    /// <param name="validationReport">The non-fatal validation report.</param>
    public CompiledCircuit(
        Circuit sourceCircuit,
        Netlist netlist,
        NodeGraph graph,
        IEnumerable<CompiledComponent> components,
        MnaVariableMap variableMap,
        CircuitValidationReport validationReport)
    {
        ArgumentNullException.ThrowIfNull(sourceCircuit);
        ArgumentNullException.ThrowIfNull(netlist);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(variableMap);
        ArgumentNullException.ThrowIfNull(validationReport);

        var componentArray = components.ToArray();
        SourceCircuit = sourceCircuit;
        Netlist = netlist;
        Graph = graph;
        Components = new ReadOnlyCollection<CompiledComponent>(componentArray);
        VariableMap = variableMap;
        ValidationReport = validationReport;
        _componentsById = componentArray.ToDictionary(component => component.ComponentId);
    }

    /// <summary>
    /// Gets the source physical circuit snapshot.
    /// </summary>
    public Circuit SourceCircuit { get; }

    /// <summary>
    /// Gets the compiled terminal-to-node netlist.
    /// </summary>
    public Netlist Netlist { get; }

    /// <summary>
    /// Gets the compiled node graph.
    /// </summary>
    public NodeGraph Graph { get; }

    /// <summary>
    /// Gets the compiled components.
    /// </summary>
    public IReadOnlyList<CompiledComponent> Components { get; }

    /// <summary>
    /// Gets the deterministic MNA variable map.
    /// </summary>
    public MnaVariableMap VariableMap { get; }

    /// <summary>
    /// Gets non-fatal validation warnings attached to the compiled circuit.
    /// </summary>
    public CircuitValidationReport ValidationReport { get; }

    /// <summary>
    /// Gets a compiled component by identifier.
    /// </summary>
    /// <param name="componentId">The component identifier.</param>
    /// <returns>The compiled component.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the component is not compiled.</exception>
    public CompiledComponent GetComponent(ComponentId componentId) => _componentsById[componentId];
}
