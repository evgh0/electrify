using System.Collections.ObjectModel;
using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Simulation;

namespace CircuitSimulator.Core.Results;

/// <summary>
/// Immutable result of a linear DC operating-point solve.
/// </summary>
public sealed class DcOperatingPointResult
{
    private readonly double[] _solution;
    private readonly IReadOnlyList<double> _solutionView;
    private readonly IReadOnlyList<NodeVoltageResult> _nodeVoltages;
    private readonly IReadOnlyList<BranchCurrentResult> _branchCurrents;

    /// <summary>
    /// Initializes a new DC operating-point result.
    /// </summary>
    /// <param name="circuit">The compiled circuit.</param>
    /// <param name="linearSystem">The assembled linear system.</param>
    /// <param name="solution">The solved unknown vector.</param>
    public DcOperatingPointResult(CompiledCircuit circuit, MnaLinearSystem linearSystem, double[] solution)
    {
        Guard.NotNull(circuit, nameof(circuit));
        Guard.NotNull(linearSystem, nameof(linearSystem));
        Guard.NotNull(solution, nameof(solution));

        if (solution.Length != circuit.VariableMap.Dimension)
        {
            throw new ArgumentException("Solution length must match the circuit variable-map dimension.", nameof(solution));
        }

        CompiledCircuit = circuit;
        LinearSystem = linearSystem;
        _solution = (double[])solution.Clone();
        _solutionView = new ReadOnlyCollection<double>(_solution);
        _nodeVoltages = BuildNodeVoltages();
        _branchCurrents = BuildBranchCurrents();
    }

    /// <summary>
    /// Gets the compiled circuit used for this solve.
    /// </summary>
    public CompiledCircuit CompiledCircuit { get; }

    /// <summary>
    /// Gets the assembled MNA linear system.
    /// </summary>
    public MnaLinearSystem LinearSystem { get; }

    /// <summary>
    /// Gets the solved unknown vector in MNA variable order.
    /// </summary>
    public IReadOnlyList<double> Solution => _solutionView;

    /// <summary>
    /// Gets solved node voltages, including ground at 0 V.
    /// </summary>
    public IReadOnlyList<NodeVoltageResult> NodeVoltages => _nodeVoltages;

    /// <summary>
    /// Gets solved branch currents for voltage sources, inductors, and ideal switches.
    /// </summary>
    public IReadOnlyList<BranchCurrentResult> BranchCurrents => _branchCurrents;

    /// <summary>
    /// Gets the voltage of a compiled node relative to ground.
    /// </summary>
    /// <param name="nodeId">The compiled node identifier.</param>
    /// <returns>The node voltage in volts.</returns>
    public double GetNodeVoltage(NodeId nodeId)
    {
        var index = CompiledCircuit.VariableMap.GetNodeVoltageIndex(nodeId);
        return index is { } variableIndex ? _solution[variableIndex.Value] : 0.0;
    }

    /// <summary>
    /// Gets the voltage of a physical terminal relative to ground.
    /// </summary>
    /// <param name="terminalId">The terminal identifier.</param>
    /// <returns>The terminal voltage in volts.</returns>
    public double GetTerminalVoltage(TerminalId terminalId) =>
        GetNodeVoltage(CompiledCircuit.Netlist.GetNode(terminalId));

    /// <summary>
    /// Gets the branch current for a voltage source, inductor, or ideal switch.
    /// </summary>
    /// <param name="componentId">The voltage-source component identifier.</param>
    /// <returns>The branch current in amperes, positive from terminal 0 to terminal 1.</returns>
    public double GetBranchCurrent(ComponentId componentId)
    {
        var index = CompiledCircuit.VariableMap.GetBranchCurrentIndex(componentId);
        return _solution[index.Value];
    }

    /// <summary>
    /// Attempts to get the branch current for a component.
    /// </summary>
    /// <param name="componentId">The component identifier.</param>
    /// <param name="current">The branch current when one exists.</param>
    /// <returns><see langword="true"/> when the component has a branch-current variable.</returns>
    public bool TryGetBranchCurrent(ComponentId componentId, out double current)
    {
        if (CompiledCircuit.VariableMap.TryGetBranchCurrentIndex(componentId, out var index))
        {
            current = _solution[index.Value];
            return true;
        }

        current = 0.0;
        return false;
    }

    /// <summary>
    /// Gets the component voltage using terminal 0 minus terminal 1.
    /// </summary>
    /// <param name="componentId">The component identifier.</param>
    /// <returns>The component voltage in volts.</returns>
    public double GetComponentVoltage(ComponentId componentId)
    {
        var component = CompiledCircuit.GetComponent(componentId);
        EnsureTwoTerminal(component);
        return GetNodeVoltage(component.Nodes[0]) - GetNodeVoltage(component.Nodes[1]);
    }

    /// <summary>
    /// Gets the component current using the positive-to-negative terminal convention.
    /// </summary>
    /// <param name="componentId">The component identifier.</param>
    /// <returns>The component current in amperes.</returns>
    public double GetComponentCurrent(ComponentId componentId)
    {
        var component = CompiledCircuit.GetComponent(componentId);
        EnsureTwoTerminal(component);

        return component.Parameters switch
        {
            ResistorParameters resistor => GetComponentVoltage(componentId) / resistor.Resistance,
            CurrentSourceParameters currentSource => currentSource.Current,
            VoltageSourceParameters => GetBranchCurrent(componentId),
            CapacitorParameters => 0.0,
            InductorParameters => GetBranchCurrent(componentId),
            DiodeParameters diode => DiodeModel.Evaluate(diode, GetComponentVoltage(componentId)).Current,
            SwitchParameters => GetBranchCurrent(componentId),
            _ => throw new SimulationException($"Component '{component.Name}' ({component.ComponentId}) has unsupported parameters.")
        };
    }

    private IReadOnlyList<NodeVoltageResult> BuildNodeVoltages()
    {
        var nodeVoltages = CompiledCircuit.Netlist.Nodes
            .OrderBy(node => node.Id.Value)
            .Select(node => new NodeVoltageResult(node.Id, GetNodeVoltage(node.Id)))
            .ToArray();

        return new ReadOnlyCollection<NodeVoltageResult>(nodeVoltages);
    }

    private IReadOnlyList<BranchCurrentResult> BuildBranchCurrents()
    {
        var branchCurrents = CompiledCircuit.VariableMap.Variables
            .Where(variable => variable.Kind == MnaVariableKind.BranchCurrent && variable.ComponentId.HasValue)
            .Select(variable => new BranchCurrentResult(variable.ComponentId!.Value, _solution[variable.Index.Value]))
            .ToArray();

        return new ReadOnlyCollection<BranchCurrentResult>(branchCurrents);
    }

    private static void EnsureTwoTerminal(CompiledComponent component)
    {
        if (component.Nodes.Count != 2)
        {
            throw new SimulationException($"Component '{component.Name}' ({component.ComponentId}) is not a two-terminal component.");
        }
    }
}
