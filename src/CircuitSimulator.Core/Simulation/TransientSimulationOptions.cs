namespace CircuitSimulator.Core.Simulation;

/// <summary>Defines fixed-step backward-Euler transient simulation settings.</summary>
public sealed record TransientSimulationOptions
{
    /// <summary>Initializes transient simulation settings.</summary>
    public TransientSimulationOptions(
        double startTime,
        double stopTime,
        double timeStep,
        TransientInitialConditions? initialConditions = null,
        NewtonRaphsonOptions? newtonOptions = null)
    {
        if (!double.IsFinite(startTime) || startTime < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTime), startTime, "Start time must be finite and non-negative.");
        }

        if (!double.IsFinite(stopTime) || stopTime <= startTime)
        {
            throw new ArgumentOutOfRangeException(nameof(stopTime), stopTime, "Stop time must be finite and greater than start time.");
        }

        if (!double.IsFinite(timeStep) || timeStep <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeStep), timeStep, "Time step must be finite and greater than zero.");
        }

        StartTime = startTime;
        StopTime = stopTime;
        TimeStep = timeStep;
        InitialConditions = initialConditions ?? TransientInitialConditions.Zero;
        NewtonOptions = newtonOptions ?? NewtonRaphsonOptions.Default;
    }

    /// <summary>Gets the initial time in seconds.</summary>
    public double StartTime { get; }

    /// <summary>Gets the final time in seconds.</summary>
    public double StopTime { get; }

    /// <summary>Gets the preferred fixed time step in seconds.</summary>
    public double TimeStep { get; }

    /// <summary>Gets explicit reactive initial conditions.</summary>
    public TransientInitialConditions InitialConditions { get; }

    /// <summary>Gets nonlinear convergence options.</summary>
    public NewtonRaphsonOptions NewtonOptions { get; }
}
