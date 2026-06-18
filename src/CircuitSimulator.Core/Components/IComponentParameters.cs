namespace CircuitSimulator.Core.Components;

/// <summary>
/// Describes immutable parameters for a component definition.
/// </summary>
public interface IComponentParameters
{
    /// <summary>
    /// Gets the component kind represented by these parameters.
    /// </summary>
    ComponentKind Kind { get; }
}
