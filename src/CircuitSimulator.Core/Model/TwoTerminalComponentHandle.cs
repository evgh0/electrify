namespace CircuitSimulator.Core.Model;

/// <summary>
/// Provides stable access to a two-terminal component and its ordered terminals.
/// </summary>
/// <param name="ComponentId">The component identifier.</param>
/// <param name="Positive">Terminal 0, the positive/reference terminal.</param>
/// <param name="Negative">Terminal 1, the negative terminal.</param>
public readonly record struct TwoTerminalComponentHandle(
    ComponentId ComponentId,
    TerminalId Positive,
    TerminalId Negative);
