using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Simulation;

/// <summary>Marks state owned by one simulation run.</summary>
public interface IComponentState;

/// <summary>Committed backward-Euler history for a capacitor.</summary>
public sealed record CapacitorState(double PreviousVoltage, double PreviousCurrent) : IComponentState;

/// <summary>Committed backward-Euler history for an inductor.</summary>
public sealed record InductorState(double PreviousCurrent) : IComponentState;

/// <summary>Owns committed component history for one transient simulation run.</summary>
public sealed class SimulationState
{
    private readonly Dictionary<ComponentId, CapacitorState> _capacitors;
    private readonly Dictionary<ComponentId, InductorState> _inductors;
    private readonly Dictionary<ComponentId, bool> _switches;
    private readonly object _switchLock = new();

    private SimulationState(
        CompiledCircuit circuit,
        Dictionary<ComponentId, CapacitorState> capacitors,
        Dictionary<ComponentId, InductorState> inductors,
        Dictionary<ComponentId, bool> switches)
    {
        Circuit = circuit;
        _capacitors = capacitors;
        _inductors = inductors;
        _switches = switches;
    }

    /// <summary>Gets the compiled circuit associated with this state.</summary>
    public CompiledCircuit Circuit { get; }

    /// <summary>Creates independent state for a compiled circuit.</summary>
    public static SimulationState Create(
        CompiledCircuit circuit,
        TransientInitialConditions? initialConditions = null)
    {
        Guard.NotNull(circuit, nameof(circuit));
        initialConditions ??= TransientInitialConditions.Zero;
        ValidateInitialConditionIds(circuit, initialConditions.CapacitorVoltages, ComponentKind.Capacitor);
        ValidateInitialConditionIds(circuit, initialConditions.InductorCurrents, ComponentKind.Inductor);

        var capacitors = circuit.Components
            .Where(component => component.Kind == ComponentKind.Capacitor)
            .ToDictionary(
                component => component.ComponentId,
                component => new CapacitorState(
                    initialConditions.CapacitorVoltages.GetValueOrDefault(component.ComponentId),
                    0.0));
        var inductors = circuit.Components
            .Where(component => component.Kind == ComponentKind.Inductor)
            .ToDictionary(
                component => component.ComponentId,
                component => new InductorState(
                    initialConditions.InductorCurrents.GetValueOrDefault(component.ComponentId)));

        var switches = circuit.Components
            .Where(component => component.Kind == ComponentKind.Switch)
            .ToDictionary(
                component => component.ComponentId,
                component => ((SwitchParameters)component.Parameters).InitiallyClosed);

        return new SimulationState(circuit, capacitors, inductors, switches);
    }

    /// <summary>Gets committed capacitor history.</summary>
    public CapacitorState GetCapacitorState(ComponentId componentId) =>
        _capacitors.TryGetValue(componentId, out var state)
            ? state
            : throw new SimulationException($"Component {componentId} has no capacitor state.");

    /// <summary>Gets committed inductor history.</summary>
    public InductorState GetInductorState(ComponentId componentId) =>
        _inductors.TryGetValue(componentId, out var state)
            ? state
            : throw new SimulationException($"Component {componentId} has no inductor state.");

    /// <summary>Gets the current control state of an ideal switch.</summary>
    /// <exception cref="SimulationException">Thrown when the component is missing or is not a switch.</exception>
    public bool GetSwitchState(ComponentId componentId)
    {
        EnsureSwitchComponent(componentId);
        lock (_switchLock)
        {
            return _switches[componentId];
        }
    }

    /// <summary>Attempts to get the current control state of an ideal switch.</summary>
    public bool TryGetSwitchState(ComponentId componentId, out bool isClosed)
    {
        lock (_switchLock)
        {
            return _switches.TryGetValue(componentId, out isClosed);
        }
    }

    /// <summary>Changes an ideal switch state for the next simulation step.</summary>
    /// <exception cref="SimulationException">Thrown when the component is missing or is not a switch.</exception>
    public void SetSwitchState(ComponentId componentId, bool isClosed)
    {
        EnsureSwitchComponent(componentId);
        lock (_switchLock)
        {
            _switches[componentId] = isClosed;
        }
    }

    internal IReadOnlyDictionary<ComponentId, bool> CaptureSwitchStates()
    {
        lock (_switchLock)
        {
            return new Dictionary<ComponentId, bool>(_switches);
        }
    }

    private void EnsureSwitchComponent(ComponentId componentId)
    {
        CompiledComponent component;
        try
        {
            component = Circuit.GetComponent(componentId);
        }
        catch (KeyNotFoundException exception)
        {
            throw new SimulationException($"Switch state references missing component {componentId}.", exception);
        }

        if (component.Kind != ComponentKind.Switch)
        {
            throw new SimulationException(
                $"Switch state requires kind {ComponentKind.Switch}, but component '{component.Name}' ({componentId}) is {component.Kind}.");
        }
    }

    internal void Commit(IReadOnlyList<double> solution, double timeStep)
    {
        Guard.NotNull(solution, nameof(solution));
        if (solution.Count != Circuit.VariableMap.Dimension)
        {
            throw new ArgumentException("Solution length must match the MNA variable-map dimension.", nameof(solution));
        }

        var capacitorUpdates = new Dictionary<ComponentId, CapacitorState>();
        var inductorUpdates = new Dictionary<ComponentId, InductorState>();

        foreach (var component in Circuit.Components)
        {
            switch (component.Parameters)
            {
                case CapacitorParameters capacitor:
                    {
                        var previous = GetCapacitorState(component.ComponentId);
                        var voltage = GetComponentVoltage(component, solution);
                        var current = (capacitor.Capacitance / timeStep) * (voltage - previous.PreviousVoltage);
                        EnsureFinite(component, voltage, current);
                        capacitorUpdates.Add(component.ComponentId, new CapacitorState(voltage, current));
                        break;
                    }

                case InductorParameters:
                    {
                        var index = Circuit.VariableMap.GetBranchCurrentIndex(component.ComponentId);
                        var current = solution[index.Value];
                        EnsureFinite(component, current);
                        inductorUpdates.Add(component.ComponentId, new InductorState(current));
                        break;
                    }
            }
        }

        foreach (var pair in capacitorUpdates)
        {
            _capacitors[pair.Key] = pair.Value;
        }

        foreach (var pair in inductorUpdates)
        {
            _inductors[pair.Key] = pair.Value;
        }
    }

    private static void ValidateInitialConditionIds(
        CompiledCircuit circuit,
        IReadOnlyDictionary<ComponentId, double> values,
        ComponentKind expectedKind)
    {
        foreach (var componentId in values.Keys)
        {
            CompiledComponent component;
            try
            {
                component = circuit.GetComponent(componentId);
            }
            catch (KeyNotFoundException exception)
            {
                throw new SimulationException($"Initial condition references missing component {componentId}.", exception);
            }

            if (component.Kind != expectedKind)
            {
                throw new SimulationException(
                    $"Initial condition for {componentId} requires kind {expectedKind}, but the component is {component.Kind}.");
            }
        }
    }

    private double GetComponentVoltage(CompiledComponent component, IReadOnlyList<double> solution) =>
        GetNodeVoltage(component.Nodes[0], solution) - GetNodeVoltage(component.Nodes[1], solution);

    private double GetNodeVoltage(NodeId nodeId, IReadOnlyList<double> solution)
    {
        var index = Circuit.VariableMap.GetNodeVoltageIndex(nodeId);
        return index is null ? 0.0 : solution[index.Value.Value];
    }

    private static void EnsureFinite(CompiledComponent component, params double[] values)
    {
        if (values.Any(value => !Guard.IsFinite(value)))
        {
            throw new SimulationException(
                $"State update for component '{component.Name}' ({component.ComponentId}) produced a non-finite value.");
        }
    }
}
