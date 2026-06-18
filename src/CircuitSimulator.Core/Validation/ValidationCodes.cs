namespace CircuitSimulator.Core.Validation;

/// <summary>
/// Stable validation issue codes emitted by circuit compilation and simulation setup.
/// </summary>
public static class ValidationCodes
{
    /// <summary>
    /// The circuit has no ground marker.
    /// </summary>
    public const string CircuitNoGround = "CIRCUIT_NO_GROUND";

    /// <summary>
    /// Ground markers compile into more than one disconnected electrical node.
    /// </summary>
    public const string CircuitMultipleDisconnectedGrounds = "CIRCUIT_MULTIPLE_DISCONNECTED_GROUNDS";

    /// <summary>
    /// A terminal references an invalid owner component.
    /// </summary>
    public const string TerminalInvalidOwner = "TERMINAL_INVALID_OWNER";

    /// <summary>
    /// A component has an invalid terminal count or terminal ordering.
    /// </summary>
    public const string ComponentInvalidTerminalCount = "COMPONENT_INVALID_TERMINAL_COUNT";

    /// <summary>
    /// A component parameter is invalid or unsupported.
    /// </summary>
    public const string ComponentInvalidParameter = "COMPONENT_INVALID_PARAMETER";

    /// <summary>
    /// A compiled node subnetwork is not connected to ground.
    /// </summary>
    public const string NodeFloatingSubnetwork = "NODE_FLOATING_SUBNETWORK";

    /// <summary>
    /// A nonzero ideal voltage source has both terminals on the same compiled node.
    /// </summary>
    public const string VoltageSourceSelfLoopNonzero = "VOLTAGE_SOURCE_SELF_LOOP_NONZERO";

    /// <summary>
    /// The compiled topology or assembled MNA system is known to be singular.
    /// </summary>
    public const string MnaSingularSystem = "MNA_SINGULAR_SYSTEM";

    /// <summary>
    /// A passive or source component has both terminals on the same compiled node.
    /// </summary>
    public const string ComponentSelfLoop = "COMPONENT_SELF_LOOP";
}
