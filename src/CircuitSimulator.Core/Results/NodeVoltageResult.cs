using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Results;

/// <summary>
/// Stores the solved voltage of one compiled electrical node.
/// </summary>
/// <param name="NodeId">The compiled node identifier.</param>
/// <param name="Voltage">The node voltage relative to ground, in volts.</param>
public sealed record NodeVoltageResult(NodeId NodeId, double Voltage);
