using System.Collections.ObjectModel;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Simulation;
using CircuitSimulator.Editor.Contracts.Documents;

namespace CircuitSimulator.Editor.Documents;

/// <summary>Defines one deterministic, rebuildable transient demonstration.</summary>
public sealed class DemoCircuitDefinition
{
    private readonly Func<DemoCircuitDefinition, DemoParameterValues, DemoCircuitInstance> _build;

    /// <summary>Initializes a demo definition.</summary>
    public DemoCircuitDefinition(
        string id,
        string displayName,
        string description,
        IEnumerable<DemoParameterDefinition> parameters,
        DemoComponentKey defaultSelectedComponent,
        Func<DemoCircuitDefinition, DemoParameterValues, DemoCircuitInstance> build)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(parameters);
        Id = id;
        DisplayName = displayName;
        Description = description;
        Parameters = new ReadOnlyCollection<DemoParameterDefinition>(parameters.ToArray());
        DefaultSelectedComponent = defaultSelectedComponent;
        _build = build ?? throw new ArgumentNullException(nameof(build));
        Defaults = new DemoParameterValues(Parameters);
    }

    /// <summary>Gets the stable example identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the display name.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the concise description.</summary>
    public string Description { get; }

    /// <summary>Gets editable parameters.</summary>
    public IReadOnlyList<DemoParameterDefinition> Parameters { get; }

    /// <summary>Gets the default selected component.</summary>
    public DemoComponentKey DefaultSelectedComponent { get; }

    /// <summary>Gets default parameter values.</summary>
    public DemoParameterValues Defaults { get; }

    /// <summary>Builds an immutable circuit and layout snapshot.</summary>
    public DemoCircuitInstance Build(DemoParameterValues values) => _build(this, values);
}

/// <summary>Contains one built demo circuit, schematic, and simulation configuration.</summary>
public sealed class DemoCircuitInstance
{
    private readonly IReadOnlyDictionary<DemoComponentKey, ComponentId> _componentIds;

    /// <summary>Initializes a built demo instance.</summary>
    public DemoCircuitInstance(
        DemoCircuitDefinition definition,
        DemoParameterValues parameterValues,
        Circuit circuit,
        DemoSchematic schematic,
        IReadOnlyDictionary<DemoComponentKey, ComponentId> componentIds,
        TransientSimulationOptions simulationOptions,
        DemoComponentKey referenceVoltageComponent)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ParameterValues = parameterValues ?? throw new ArgumentNullException(nameof(parameterValues));
        Circuit = circuit ?? throw new ArgumentNullException(nameof(circuit));
        Schematic = schematic ?? throw new ArgumentNullException(nameof(schematic));
        ArgumentNullException.ThrowIfNull(componentIds);
        _componentIds = new ReadOnlyDictionary<DemoComponentKey, ComponentId>(
            new Dictionary<DemoComponentKey, ComponentId>(componentIds));
        SimulationOptions = simulationOptions ?? throw new ArgumentNullException(nameof(simulationOptions));
        ReferenceVoltageComponent = referenceVoltageComponent;
    }

    /// <summary>Gets the originating definition.</summary>
    public DemoCircuitDefinition Definition { get; }

    /// <summary>Gets applied parameter values.</summary>
    public DemoParameterValues ParameterValues { get; }

    /// <summary>Gets the immutable physical circuit.</summary>
    public Circuit Circuit { get; }

    /// <summary>Gets the fixed schematic.</summary>
    public DemoSchematic Schematic { get; }

    /// <summary>Gets transient settings.</summary>
    public TransientSimulationOptions SimulationOptions { get; }

    /// <summary>Gets the component used as a voltage reference trace.</summary>
    public DemoComponentKey ReferenceVoltageComponent { get; }

    /// <summary>Gets a physical component identifier from a stable key.</summary>
    public ComponentId GetComponentId(DemoComponentKey key) => _componentIds[key];

    /// <summary>Gets a stable key from a physical component identifier.</summary>
    public DemoComponentKey GetComponentKey(ComponentId componentId) =>
        _componentIds.First(pair => pair.Value == componentId).Key;
}
