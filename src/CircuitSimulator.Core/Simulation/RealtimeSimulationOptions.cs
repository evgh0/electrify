namespace CircuitSimulator.Core.Simulation;

/// <summary>Defines ongoing fixed-step realtime simulation settings.</summary>
public sealed record RealtimeSimulationOptions
{
    /// <summary>Initializes ongoing realtime simulation settings.</summary>
    public RealtimeSimulationOptions(
        double timeStep,
        TransientInitialConditions? initialConditions = null,
        NewtonRaphsonOptions? newtonOptions = null)
    {
        if (!Guard.IsFinite(timeStep) || timeStep <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeStep), timeStep, "Time step must be finite and greater than zero.");
        }

        TimeStep = timeStep;
        InitialConditions = initialConditions ?? TransientInitialConditions.Zero;
        NewtonOptions = newtonOptions ?? NewtonRaphsonOptions.Default;
    }

    /// <summary>Gets the fixed backward-Euler integration step in seconds.</summary>
    public double TimeStep { get; }

    /// <summary>Gets explicit reactive initial conditions.</summary>
    public TransientInitialConditions InitialConditions { get; }

    /// <summary>Gets nonlinear convergence options.</summary>
    public NewtonRaphsonOptions NewtonOptions { get; }
}
