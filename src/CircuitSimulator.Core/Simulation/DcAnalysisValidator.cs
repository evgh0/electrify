using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Simulation;

internal static class DcAnalysisValidator
{
    public static void Validate(CompiledCircuit circuit)
    {
        foreach (var component in circuit.Components)
        {
            if (component.Kind == ComponentKind.Inductor && component.Nodes[0] == component.Nodes[1])
            {
                throw new SimulationException(
                    $"Inductor '{component.Name}' ({component.ComponentId}) is a DC ideal-short self-loop and creates a singular branch-current constraint.");
            }
        }

        var adjacency = circuit.Netlist.Nodes.ToDictionary(node => node.Id, static _ => new HashSet<NodeId>());
        foreach (var component in circuit.Components)
        {
            if (component.Kind is ComponentKind.Capacitor or ComponentKind.CurrentSource ||
                component.Parameters is SwitchParameters { InitiallyClosed: false } ||
                component.Nodes[0] == component.Nodes[1])
            {
                continue;
            }

            adjacency[component.Nodes[0]].Add(component.Nodes[1]);
            adjacency[component.Nodes[1]].Add(component.Nodes[0]);
        }

        var reachable = new HashSet<NodeId> { NodeId.Ground };
        var pending = new Stack<NodeId>();
        pending.Push(NodeId.Ground);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var next in adjacency[current])
            {
                if (reachable.Add(next))
                {
                    pending.Push(next);
                }
            }
        }

        var floating = circuit.Netlist.Nodes
            .Select(node => node.Id)
            .Where(node => !reachable.Contains(node))
            .OrderBy(node => node.Value)
            .ToArray();
        if (floating.Length > 0)
        {
            throw new SimulationException(
                $"DC analysis has nodes disconnected from ground after capacitors, independent current sources, and initially open switches are treated as open branches: {string.Join(", ", floating)}.");
        }
    }
}
