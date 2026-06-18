using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Mna;
using CircuitSimulator.Core.Model;
using CircuitSimulator.Core.Topology;
using CircuitSimulator.Core.Validation;

namespace CircuitSimulator.Core.Compilation;

/// <summary>
/// Compiles physical circuit terminals and ideal wires into deterministic electrical nodes.
/// </summary>
public sealed class CircuitCompiler
{
    /// <summary>
    /// Compiles a physical circuit into immutable topology and MNA variable metadata.
    /// </summary>
    /// <param name="circuit">The physical circuit.</param>
    /// <returns>The compiled circuit.</returns>
    /// <exception cref="CircuitCompilationException">Thrown when compilation validation fails.</exception>
    public CompiledCircuit Compile(Circuit circuit)
    {
        ArgumentNullException.ThrowIfNull(circuit);

        var issues = new List<CircuitValidationIssue>();
        ValidatePhysicalCircuit(circuit, issues);
        ThrowIfErrors(issues);

        var orderedTerminals = circuit.Terminals.OrderBy(terminal => terminal.Id.Value).ToArray();
        var terminalPositions = orderedTerminals
            .Select((terminal, index) => (terminal.Id, Index: index))
            .ToDictionary(pair => pair.Id, pair => pair.Index);

        var disjointSet = new DisjointSet(orderedTerminals.Length);
        foreach (var wire in circuit.Wires)
        {
            if (wire.First == wire.Second)
            {
                continue;
            }

            disjointSet.Union(terminalPositions[wire.First], terminalPositions[wire.Second]);
        }

        var groups = BuildTerminalGroups(orderedTerminals, terminalPositions, disjointSet);
        var groundRepresentatives = circuit.GroundTerminals
            .Select(terminal => disjointSet.Find(terminalPositions[terminal]))
            .Distinct()
            .ToArray();

        if (groundRepresentatives.Length > 1)
        {
            issues.Add(new CircuitValidationIssue(
                ValidationCodes.CircuitMultipleDisconnectedGrounds,
                ValidationSeverity.Error,
                "Ground markers compile into multiple disconnected electrical nodes."));
            ThrowIfErrors(issues);
        }

        var groundRepresentative = groundRepresentatives.Single();
        var terminalToNode = new Dictionary<TerminalId, NodeId>();
        var nodes = BuildNodes(groups, groundRepresentative, terminalToNode);
        var netlist = new Netlist(nodes, terminalToNode, NodeId.Ground);
        var compiledComponents = CompileComponents(circuit, netlist);
        var graph = new NodeGraph(
            nodes,
            compiledComponents.Select(component => new ComponentGraphEdge(component.ComponentId, component.Nodes)));

        ValidateCompiledCircuit(compiledComponents, graph, issues);
        ThrowIfErrors(issues);

        var variableMap = MnaVariableMap.Create(nodes, compiledComponents);
        var report = new CircuitValidationReport(issues);

        return new CompiledCircuit(circuit, netlist, graph, compiledComponents, variableMap, report);
    }

    private static void ValidatePhysicalCircuit(Circuit circuit, List<CircuitValidationIssue> issues)
    {
        var componentIds = circuit.Components.Select(component => component.Id).ToHashSet();
        var terminalIds = circuit.Terminals.Select(terminal => terminal.Id).ToHashSet();

        if (circuit.GroundTerminals.Count == 0)
        {
            issues.Add(new CircuitValidationIssue(
                ValidationCodes.CircuitNoGround,
                ValidationSeverity.Error,
                "Circuit compilation requires at least one ground terminal."));
        }

        foreach (var terminal in circuit.Terminals)
        {
            if (!componentIds.Contains(terminal.OwnerComponentId))
            {
                issues.Add(new CircuitValidationIssue(
                    ValidationCodes.TerminalInvalidOwner,
                    ValidationSeverity.Error,
                    $"Terminal {terminal.Id} references missing owner component {terminal.OwnerComponentId}.",
                    terminalId: terminal.Id));
            }
        }

        foreach (var component in circuit.Components)
        {
            ValidateComponent(component, circuit, terminalIds, issues);
        }

        foreach (var wire in circuit.Wires)
        {
            if (!terminalIds.Contains(wire.First))
            {
                issues.Add(new CircuitValidationIssue(
                    ValidationCodes.TerminalInvalidOwner,
                    ValidationSeverity.Error,
                    $"Wire references missing terminal {wire.First}.",
                    terminalId: wire.First));
            }

            if (!terminalIds.Contains(wire.Second))
            {
                issues.Add(new CircuitValidationIssue(
                    ValidationCodes.TerminalInvalidOwner,
                    ValidationSeverity.Error,
                    $"Wire references missing terminal {wire.Second}.",
                    terminalId: wire.Second));
            }
        }

        foreach (var groundTerminal in circuit.GroundTerminals)
        {
            if (!terminalIds.Contains(groundTerminal))
            {
                issues.Add(new CircuitValidationIssue(
                    ValidationCodes.TerminalInvalidOwner,
                    ValidationSeverity.Error,
                    $"Ground marker references missing terminal {groundTerminal}.",
                    terminalId: groundTerminal));
            }
        }
    }

    private static void ValidateComponent(
        ComponentDefinition component,
        Circuit circuit,
        HashSet<TerminalId> terminalIds,
        List<CircuitValidationIssue> issues)
    {
        if (!Enum.IsDefined(component.Kind))
        {
            issues.Add(new CircuitValidationIssue(
                ValidationCodes.ComponentInvalidParameter,
                ValidationSeverity.Error,
                $"Component '{component.Name}' ({component.Id}) has unsupported kind {component.Kind}.",
                componentId: component.Id));
            return;
        }

        if (component.TerminalIds.Count != 2)
        {
            issues.Add(new CircuitValidationIssue(
                ValidationCodes.ComponentInvalidTerminalCount,
                ValidationSeverity.Error,
                $"Component '{component.Name}' ({component.Id}) has {component.TerminalIds.Count} terminals; expected 2.",
                componentId: component.Id));
            return;
        }

        for (var localIndex = 0; localIndex < component.TerminalIds.Count; localIndex++)
        {
            var terminalId = component.TerminalIds[localIndex];
            if (!terminalIds.Contains(terminalId))
            {
                issues.Add(new CircuitValidationIssue(
                    ValidationCodes.ComponentInvalidTerminalCount,
                    ValidationSeverity.Error,
                    $"Component '{component.Name}' ({component.Id}) references missing terminal {terminalId}.",
                    componentId: component.Id,
                    terminalId: terminalId));
                continue;
            }

            var terminal = circuit.GetTerminal(terminalId);
            if (terminal.OwnerComponentId != component.Id || terminal.LocalIndex != localIndex)
            {
                issues.Add(new CircuitValidationIssue(
                    ValidationCodes.TerminalInvalidOwner,
                    ValidationSeverity.Error,
                    $"Terminal {terminal.Id} is not local index {localIndex} of component '{component.Name}' ({component.Id}).",
                    componentId: component.Id,
                    terminalId: terminal.Id));
            }
        }

        if (!ParametersMatchKind(component))
        {
            issues.Add(new CircuitValidationIssue(
                ValidationCodes.ComponentInvalidParameter,
                ValidationSeverity.Error,
                $"Component '{component.Name}' ({component.Id}) has parameters incompatible with kind {component.Kind}.",
                componentId: component.Id));
        }
    }

    private static bool ParametersMatchKind(ComponentDefinition component) =>
        component.Kind switch
        {
            ComponentKind.Resistor => component.Parameters is ResistorParameters,
            ComponentKind.CurrentSource => component.Parameters is CurrentSourceParameters,
            ComponentKind.VoltageSource => component.Parameters is VoltageSourceParameters,
            _ => false
        };

    private static Dictionary<int, List<TerminalId>> BuildTerminalGroups(
        IEnumerable<TerminalDefinition> orderedTerminals,
        IReadOnlyDictionary<TerminalId, int> terminalPositions,
        DisjointSet disjointSet)
    {
        var groups = new Dictionary<int, List<TerminalId>>();
        foreach (var terminal in orderedTerminals)
        {
            var representative = disjointSet.Find(terminalPositions[terminal.Id]);
            if (!groups.TryGetValue(representative, out var group))
            {
                group = [];
                groups.Add(representative, group);
            }

            group.Add(terminal.Id);
        }

        return groups;
    }

    private static IReadOnlyList<ElectricalNode> BuildNodes(
        Dictionary<int, List<TerminalId>> groups,
        int groundRepresentative,
        Dictionary<TerminalId, NodeId> terminalToNode)
    {
        var nodes = new List<ElectricalNode>();
        AddNode(NodeId.Ground, isGround: true, groups[groundRepresentative], terminalToNode, nodes);

        var nonGroundGroups = groups
            .Where(group => group.Key != groundRepresentative)
            .OrderBy(group => group.Value.Min(terminal => terminal.Value));

        foreach (var group in nonGroundGroups)
        {
            AddNode(new NodeId(nodes.Count), isGround: false, group.Value, terminalToNode, nodes);
        }

        return nodes;
    }

    private static void AddNode(
        NodeId nodeId,
        bool isGround,
        IEnumerable<TerminalId> terminals,
        Dictionary<TerminalId, NodeId> terminalToNode,
        List<ElectricalNode> nodes)
    {
        var terminalArray = terminals.OrderBy(terminal => terminal.Value).ToArray();
        foreach (var terminalId in terminalArray)
        {
            terminalToNode.Add(terminalId, nodeId);
        }

        nodes.Add(new ElectricalNode(nodeId, isGround, terminalArray));
    }

    private static IReadOnlyList<CompiledComponent> CompileComponents(Circuit circuit, Netlist netlist) =>
        circuit.Components
            .OrderBy(component => component.Id.Value)
            .Select(component => new CompiledComponent(
                component.Id,
                component.Name,
                component.Parameters,
                component.TerminalIds.Select(netlist.GetNode)))
            .ToArray();

    private static void ValidateCompiledCircuit(
        IReadOnlyList<CompiledComponent> components,
        NodeGraph graph,
        List<CircuitValidationIssue> issues)
    {
        foreach (var component in components)
        {
            if (component.Nodes.Count != 2 || component.Nodes[0] != component.Nodes[1])
            {
                continue;
            }

            if (component.Parameters is VoltageSourceParameters voltageSource)
            {
                if (voltageSource.Voltage != 0.0)
                {
                    issues.Add(new CircuitValidationIssue(
                        ValidationCodes.VoltageSourceSelfLoopNonzero,
                        ValidationSeverity.Error,
                        $"Voltage source '{component.Name}' ({component.ComponentId}) has both terminals on {component.Nodes[0]} but specifies {voltageSource.Voltage} V.",
                        componentId: component.ComponentId,
                        nodeId: component.Nodes[0]));
                }
                else
                {
                    issues.Add(new CircuitValidationIssue(
                        ValidationCodes.MnaSingularSystem,
                        ValidationSeverity.Error,
                        $"Zero-volt voltage source '{component.Name}' ({component.ComponentId}) is a self-loop and would add a redundant MNA constraint.",
                        componentId: component.ComponentId,
                        nodeId: component.Nodes[0]));
                }
            }
            else
            {
                issues.Add(new CircuitValidationIssue(
                    ValidationCodes.ComponentSelfLoop,
                    ValidationSeverity.Warning,
                    $"Component '{component.Name}' ({component.ComponentId}) has both terminals on {component.Nodes[0]}; its stamp cancels on that node.",
                    componentId: component.ComponentId,
                    nodeId: component.Nodes[0]));
            }
        }

        foreach (var connectedComponent in graph.GetConnectedComponents())
        {
            if (connectedComponent.Contains(NodeId.Ground))
            {
                continue;
            }

            issues.Add(new CircuitValidationIssue(
                ValidationCodes.NodeFloatingSubnetwork,
                ValidationSeverity.Error,
                $"Node subnetwork beginning at {connectedComponent[0]} is not connected to ground.",
                nodeId: connectedComponent[0]));
        }
    }

    private static void ThrowIfErrors(List<CircuitValidationIssue> issues)
    {
        var report = new CircuitValidationReport(issues);
        if (report.HasErrors)
        {
            throw new CircuitCompilationException("Circuit compilation failed validation.", report);
        }
    }
}
