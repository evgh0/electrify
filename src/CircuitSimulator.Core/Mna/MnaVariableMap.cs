using System.Collections.ObjectModel;
using CircuitSimulator.Core.Compilation;
using CircuitSimulator.Core.Components;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Mna;

/// <summary>
/// Provides deterministic mappings between compiled circuit entities and MNA unknown indexes.
/// </summary>
public sealed class MnaVariableMap
{
    private readonly Dictionary<NodeId, VariableIndex> _nodeVoltageIndexes;
    private readonly Dictionary<ComponentId, VariableIndex> _branchCurrentIndexes;

    private MnaVariableMap(
        IReadOnlyList<MnaVariable> variables,
        Dictionary<NodeId, VariableIndex> nodeVoltageIndexes,
        Dictionary<ComponentId, VariableIndex> branchCurrentIndexes)
    {
        Variables = variables;
        _nodeVoltageIndexes = nodeVoltageIndexes;
        _branchCurrentIndexes = branchCurrentIndexes;
    }

    /// <summary>
    /// Gets the MNA system dimension.
    /// </summary>
    public int Dimension => Variables.Count;

    /// <summary>
    /// Gets allocated variables in deterministic unknown-vector order.
    /// </summary>
    public IReadOnlyList<MnaVariable> Variables { get; }

    /// <summary>
    /// Creates a deterministic variable map.
    /// </summary>
    /// <param name="nodes">The compiled nodes.</param>
    /// <param name="components">The compiled components.</param>
    /// <returns>The variable map.</returns>
    public static MnaVariableMap Create(
        IEnumerable<ElectricalNode> nodes,
        IEnumerable<CompiledComponent> components)
    {
        Guard.NotNull(nodes, nameof(nodes));
        Guard.NotNull(components, nameof(components));

        var variables = new List<MnaVariable>();
        var nodeVoltageIndexes = new Dictionary<NodeId, VariableIndex>();
        var branchCurrentIndexes = new Dictionary<ComponentId, VariableIndex>();

        foreach (var node in nodes.OrderBy(node => node.Id.Value))
        {
            if (node.IsGround)
            {
                continue;
            }

            var index = new VariableIndex(variables.Count);
            nodeVoltageIndexes.Add(node.Id, index);
            variables.Add(new MnaVariable(index, MnaVariableKind.NodeVoltage, node.Id, null, $"V({node.Id})"));
        }

        foreach (var component in components
                     .Where(static component => component.Kind == ComponentKind.VoltageSource)
                     .OrderBy(component => component.ComponentId.Value))
        {
            var index = new VariableIndex(variables.Count);
            branchCurrentIndexes.Add(component.ComponentId, index);
            variables.Add(new MnaVariable(index, MnaVariableKind.BranchCurrent, null, component.ComponentId, $"I({component.Name})"));
        }

        foreach (var component in components
                     .Where(static component => component.Kind == ComponentKind.Inductor)
                     .OrderBy(component => component.ComponentId.Value))
        {
            var index = new VariableIndex(variables.Count);
            branchCurrentIndexes.Add(component.ComponentId, index);
            variables.Add(new MnaVariable(index, MnaVariableKind.BranchCurrent, null, component.ComponentId, $"I({component.Name})"));
        }

        return new MnaVariableMap(
            new ReadOnlyCollection<MnaVariable>(variables),
            nodeVoltageIndexes,
            branchCurrentIndexes);
    }

    /// <summary>
    /// Gets the node-voltage variable for a compiled node.
    /// </summary>
    /// <param name="nodeId">The compiled node identifier.</param>
    /// <returns>The variable index, or <see langword="null"/> for ground.</returns>
    public VariableIndex? GetNodeVoltageIndex(NodeId nodeId)
    {
        if (nodeId == NodeId.Ground)
        {
            return null;
        }

        return _nodeVoltageIndexes.TryGetValue(nodeId, out var index)
            ? index
            : throw new MnaAssemblyException($"No node-voltage variable is allocated for {nodeId}.");
    }

    /// <summary>
    /// Gets the branch-current variable for a component.
    /// </summary>
    /// <param name="componentId">The component identifier.</param>
    /// <returns>The branch-current variable index.</returns>
    /// <exception cref="MnaAssemblyException">Thrown when the component has no branch-current variable.</exception>
    public VariableIndex GetBranchCurrentIndex(ComponentId componentId)
    {
        if (_branchCurrentIndexes.TryGetValue(componentId, out var index))
        {
            return index;
        }

        throw new MnaAssemblyException($"No branch-current variable is allocated for component {componentId}.");
    }

    /// <summary>
    /// Attempts to get the branch-current variable for a component.
    /// </summary>
    /// <param name="componentId">The component identifier.</param>
    /// <param name="index">The branch-current variable index when present.</param>
    /// <returns><see langword="true"/> when a branch-current variable exists.</returns>
    public bool TryGetBranchCurrentIndex(ComponentId componentId, out VariableIndex index) =>
        _branchCurrentIndexes.TryGetValue(componentId, out index);
}
