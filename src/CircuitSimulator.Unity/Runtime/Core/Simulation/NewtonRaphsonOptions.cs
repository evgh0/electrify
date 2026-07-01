#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Simulation
{

/// <summary>Configures Newton-Raphson convergence and diode voltage damping.</summary>
internal sealed record NewtonRaphsonOptions
{
    /// <summary>Gets production defaults for nonlinear circuit simulation.</summary>
    public static NewtonRaphsonOptions Default { get; } = new();

    /// <summary>Initializes Newton-Raphson options.</summary>
    public NewtonRaphsonOptions(
        int maximumIterations = 50,
        double relativeTolerance = 1e-6,
        double voltageAbsoluteTolerance = 1e-9,
        double currentAbsoluteTolerance = 1e-12,
        double maximumDiodeVoltageStep = 0.25)
    {
        if (maximumIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumIterations), maximumIterations, "Maximum iterations must be greater than zero.");
        }

        ValidatePositiveFinite(relativeTolerance, nameof(relativeTolerance));
        ValidatePositiveFinite(voltageAbsoluteTolerance, nameof(voltageAbsoluteTolerance));
        ValidatePositiveFinite(currentAbsoluteTolerance, nameof(currentAbsoluteTolerance));
        ValidatePositiveFinite(maximumDiodeVoltageStep, nameof(maximumDiodeVoltageStep));
        MaximumIterations = maximumIterations;
        RelativeTolerance = relativeTolerance;
        VoltageAbsoluteTolerance = voltageAbsoluteTolerance;
        CurrentAbsoluteTolerance = currentAbsoluteTolerance;
        MaximumDiodeVoltageStep = maximumDiodeVoltageStep;
    }

    /// <summary>Gets the maximum iteration count.</summary>
    public int MaximumIterations { get; }

    /// <summary>Gets the relative correction tolerance.</summary>
    public double RelativeTolerance { get; }

    /// <summary>Gets the node-voltage absolute correction tolerance.</summary>
    public double VoltageAbsoluteTolerance { get; }

    /// <summary>Gets the branch-current absolute correction tolerance.</summary>
    public double CurrentAbsoluteTolerance { get; }

    /// <summary>Gets the maximum diode-voltage change admitted by one iteration.</summary>
    public double MaximumDiodeVoltageStep { get; }

    private static void ValidatePositiveFinite(double value, string name)
    {
        if (!Guard.IsFinite(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(name, value, "Tolerance must be finite and greater than zero.");
        }
    }
}

}
