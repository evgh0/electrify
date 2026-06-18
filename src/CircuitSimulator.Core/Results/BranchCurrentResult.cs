using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Results;

/// <summary>
/// Stores the solved branch current of an ideal voltage source.
/// </summary>
/// <param name="ComponentId">The component identifier.</param>
/// <param name="Current">The branch current in amperes, positive from terminal 0 to terminal 1.</param>
public sealed record BranchCurrentResult(ComponentId ComponentId, double Current);
