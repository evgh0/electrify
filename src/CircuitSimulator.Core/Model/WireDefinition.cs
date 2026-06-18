namespace CircuitSimulator.Core.Model;

/// <summary>
/// Defines an ideal zero-resistance wire between two physical terminals.
/// </summary>
/// <param name="First">The first terminal.</param>
/// <param name="Second">The second terminal.</param>
public sealed record WireDefinition(TerminalId First, TerminalId Second);
