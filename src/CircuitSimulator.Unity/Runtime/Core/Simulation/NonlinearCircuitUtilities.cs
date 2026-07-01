#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Simulation
{

internal static class NonlinearCircuitUtilities
{
    public static bool ContainsNonlinearComponents(CompiledCircuit circuit) =>
        circuit.Components.Any(static component => IsShockleyDevice(component.Kind));

    public static double[] LimitDiodeVoltageStep(
        CompiledCircuit circuit,
        IReadOnlyList<double> current,
        IReadOnlyList<double> candidate,
        double maximumVoltageStep)
    {
        var scale = 1.0;
        foreach (var diode in circuit.Components.Where(static component => IsShockleyDevice(component.Kind)))
        {
            var currentVoltage = GetComponentVoltage(circuit, diode, current);
            var candidateVoltage = GetComponentVoltage(circuit, diode, candidate);
            var change = Math.Abs(candidateVoltage - currentVoltage);
            if (change > maximumVoltageStep)
            {
                scale = Math.Min(scale, maximumVoltageStep / change);
            }
        }

        var limited = new double[current.Count];
        for (var index = 0; index < limited.Length; index++)
        {
            limited[index] = current[index] + (scale * (candidate[index] - current[index]));
        }

        return limited;
    }

    private static double GetComponentVoltage(
        CompiledCircuit circuit,
        CompiledComponent component,
        IReadOnlyList<double> solution) =>
        GetNodeVoltage(circuit, component.Nodes[0], solution) -
        GetNodeVoltage(circuit, component.Nodes[1], solution);

    private static double GetNodeVoltage(
        CompiledCircuit circuit,
        NodeId nodeId,
        IReadOnlyList<double> solution)
    {
        var index = circuit.VariableMap.GetNodeVoltageIndex(nodeId);
        return index is null ? 0.0 : solution[index.Value.Value];
    }

    private static bool IsShockleyDevice(ComponentKind kind) =>
        kind is ComponentKind.Diode or ComponentKind.Led;
}

}
