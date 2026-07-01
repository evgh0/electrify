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
    VoltageSource,

    /// <summary>
    /// A two-terminal capacitor.
    /// </summary>
    Capacitor,

    /// <summary>
    /// A two-terminal inductor.
    /// </summary>
    Inductor,

    /// <summary>
    /// A two-terminal Shockley diode.
    /// </summary>
    Diode,

    /// <summary>
    /// A two-terminal LED modeled as a Shockley diode fitted to a nominal forward operating point.
    /// </summary>
    Led,

    /// <summary>
    /// A controllable ideal two-terminal switch.
    /// </summary>
    Switch
}
