#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Simulation
{

/// <summary>Thrown when a nonlinear solve cannot satisfy its convergence criteria.</summary>
internal sealed class NonlinearConvergenceException : SimulationException
{
    /// <summary>Initializes a nonlinear convergence failure.</summary>
    public NonlinearConvergenceException(
        string message,
        int iterationCount,
        double maximumCorrection)
        : base(message)
    {
        IterationCount = iterationCount;
        MaximumCorrection = maximumCorrection;
    }

    /// <summary>Initializes a transient nonlinear convergence failure while preserving its cause.</summary>
    public NonlinearConvergenceException(
        string message,
        int iterationCount,
        double maximumCorrection,
        double simulationTime,
        Exception innerException)
        : base(message, innerException)
    {
        if (!Guard.IsFinite(simulationTime))
        {
            throw new ArgumentOutOfRangeException(nameof(simulationTime), simulationTime, "Simulation time must be finite.");
        }

        IterationCount = iterationCount;
        MaximumCorrection = maximumCorrection;
        SimulationTime = simulationTime;
    }

    /// <summary>Gets the number of completed Newton iterations.</summary>
    public int IterationCount { get; }

    /// <summary>Gets the final maximum absolute correction.</summary>
    public double MaximumCorrection { get; }

    /// <summary>Gets the transient time associated with the failure, when applicable.</summary>
    public double? SimulationTime { get; }
}

}
