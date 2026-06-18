namespace CircuitSimulator.Core.Mna;

/// <summary>
/// Describes the kind of an allocated MNA unknown.
/// </summary>
public enum MnaVariableKind
{
    /// <summary>
    /// A non-ground node voltage unknown.
    /// </summary>
    NodeVoltage,

    /// <summary>
    /// A branch-current unknown for an ideal voltage source.
    /// </summary>
    BranchCurrent,

    /// <summary>
    /// A reserved future internal state unknown.
    /// </summary>
    InternalState
}
