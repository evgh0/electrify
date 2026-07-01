using System.Collections.ObjectModel;
using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Simulation;

namespace CircuitSimulator.Core.Results;

/// <summary>Immutable solved sample from a backward-Euler transient simulation.</summary>
public sealed class TransientSample
{
    private readonly double[] _solution;
    private readonly IReadOnlyList<double> _solutionView;
    private readonly IReadOnlyDictionary<ComponentId, double> _componentCurrents;

    internal TransientSample(
        CompiledCircuit circuit,
        double time,
        double timeStep,
        SimulationState committedState,
        IReadOnlyList<double> solution)
    {
        CompiledCircuit = circuit;
        Time = time;
        TimeStep = timeStep;
        _solution = solution.ToArray();
        _solutionView = new ReadOnlyCollection<double>(_solution);
        _componentCurrents = new ReadOnlyDictionary<ComponentId, double>(BuildComponentCurrents(committedState));
    }

    /// <summary>Gets the compiled circuit used by this sample.</summary>
    public CompiledCircuit CompiledCircuit { get; }

    /// <summary>Gets the sample time in seconds.</summary>
    public double Time { get; }

    /// <summary>Gets the integration step that produced this sample.</summary>
    public double TimeStep { get; }

    /// <summary>Gets the solved MNA vector in deterministic variable order.</summary>
    public IReadOnlyList<double> Solution => _solutionView;

    /// <summary>Gets a node voltage relative to ground.</summary>
    public double GetNodeVoltage(NodeId nodeId)
    {
        var index = CompiledCircuit.VariableMap.GetNodeVoltageIndex(nodeId);
        return index is null ? 0.0 : _solution[index.Value.Value];
    }

    /// <summary>Gets a terminal voltage relative to ground.</summary>
    public double GetTerminalVoltage(TerminalId terminalId) =>
        GetNodeVoltage(CompiledCircuit.Netlist.GetNode(terminalId));

    /// <summary>Gets terminal-0 voltage minus terminal-1 voltage for a component.</summary>
    public double GetComponentVoltage(ComponentId componentId)
    {
        var component = CompiledCircuit.GetComponent(componentId);
        EnsureTwoTerminal(component);
        return GetNodeVoltage(component.Nodes[0]) - GetNodeVoltage(component.Nodes[1]);
    }

    /// <summary>Gets positive-to-negative current for a component at this sample.</summary>
    public double GetComponentCurrent(ComponentId componentId) =>
        _componentCurrents.TryGetValue(componentId, out var current)
            ? current
            : throw new KeyNotFoundException($"No current result exists for component {componentId}.");

    /// <summary>Gets a solved voltage-source, inductor, or switch branch current.</summary>
    public double GetBranchCurrent(ComponentId componentId)
    {
        var index = CompiledCircuit.VariableMap.GetBranchCurrentIndex(componentId);
        return _solution[index.Value];
    }

    private Dictionary<ComponentId, double> BuildComponentCurrents(SimulationState committedState)
    {
        var currents = new Dictionary<ComponentId, double>();
        foreach (var component in CompiledCircuit.Components)
        {
            var voltage = GetComponentVoltage(component.ComponentId);
            var current = component.Parameters switch
            {
                ResistorParameters resistor => voltage / resistor.Resistance,
                CurrentSourceParameters source => source.TransientWaveform.GetValue(Time),
                VoltageSourceParameters => GetBranchCurrent(component.ComponentId),
                CapacitorParameters capacitor =>
                    (capacitor.Capacitance / TimeStep) *
                    (voltage - committedState.GetCapacitorState(component.ComponentId).PreviousVoltage),
                InductorParameters => GetBranchCurrent(component.ComponentId),
                DiodeParameters diode => DiodeModel.Evaluate(diode, voltage).Current,
                LedParameters led => DiodeModel.Evaluate(led, voltage).Current,
                SwitchParameters => GetBranchCurrent(component.ComponentId),
                _ => throw new SimulationException(
                    $"Component '{component.Name}' ({component.ComponentId}) has unsupported parameters.")
            };

            if (!Guard.IsFinite(current))
            {
                throw new SimulationException(
                    $"Current for component '{component.Name}' ({component.ComponentId}) is non-finite at time {Time:R}.");
            }

            currents.Add(component.ComponentId, current);
        }

        return currents;
    }

    private static void EnsureTwoTerminal(CompiledComponent component)
    {
        if (component.Nodes.Count != 2)
        {
            throw new SimulationException(
                $"Component '{component.Name}' ({component.ComponentId}) is not a two-terminal component.");
        }
    }
}
