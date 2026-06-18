namespace CircuitSimulator.Core.Components;

/// <summary>
/// Describes the supported physical component kinds.
/// </summary>
public enum ComponentKind
{
    /// <summary>
    /// A two-terminal resistor.
    /// </summary>
    Resistor,

    /// <summary>
    /// A two-terminal independent current source.
    /// </summary>
    CurrentSource,

    /// <summary>
    /// A two-terminal independent voltage source.
    /// </summary>
    VoltageSource
}
