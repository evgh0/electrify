using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Mna;

/// <summary>
/// Describes one allocated MNA unknown.
/// </summary>
public sealed class MnaVariable
{
    /// <summary>
    /// Initializes MNA variable metadata.
    /// </summary>
    /// <param name="index">The variable index in the global unknown vector.</param>
    /// <param name="kind">The variable kind.</param>
    /// <param name="nodeId">The related node for node-voltage variables.</param>
    /// <param name="componentId">The related component for branch-current variables.</param>
    /// <param name="name">The diagnostic variable name.</param>
    public MnaVariable(
        VariableIndex index,
        MnaVariableKind kind,
        NodeId? nodeId,
        ComponentId? componentId,
        string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Variable name cannot be null or whitespace.", nameof(name));
        }

        Index = index;
        Kind = kind;
        NodeId = nodeId;
        ComponentId = componentId;
        Name = name;
    }

    /// <summary>
    /// Gets the variable index in the global unknown vector.
    /// </summary>
    public VariableIndex Index { get; }

    /// <summary>
    /// Gets the variable kind.
    /// </summary>
    public MnaVariableKind Kind { get; }

    /// <summary>
    /// Gets the related node for node-voltage variables.
    /// </summary>
    public NodeId? NodeId { get; }

    /// <summary>
    /// Gets the related component for branch-current variables.
    /// </summary>
    public ComponentId? ComponentId { get; }

    /// <summary>
    /// Gets the diagnostic variable name.
    /// </summary>
    public string Name { get; }
}
