namespace CircuitSimulator.Core.Components;

/// <summary>Evaluates the Shockley diode model and its tangent linearization.</summary>
public static class DiodeModel
{
    private const double ExponentialContinuationThreshold = 40.0;

    /// <summary>Evaluates current, conductance, and equivalent-source current at a diode voltage.</summary>
    public static DiodeLinearization Evaluate(DiodeParameters parameters, double voltage)
    {
        Guard.NotNull(parameters, nameof(parameters));
        if (!Guard.IsFinite(voltage))
        {
            throw new ArgumentOutOfRangeException(nameof(voltage), voltage, "Diode voltage must be finite.");
        }

        var voltageScale = parameters.IdealityFactor * parameters.ThermalVoltage;
        var exponent = voltage / voltageScale;
        double exponential;
        double exponentialDerivative;

        if (exponent <= ExponentialContinuationThreshold)
        {
            exponential = Math.Exp(exponent);
            exponentialDerivative = exponential;
        }
        else
        {
            var thresholdValue = Math.Exp(ExponentialContinuationThreshold);
            exponential = thresholdValue * (1.0 + exponent - ExponentialContinuationThreshold);
            exponentialDerivative = thresholdValue;
        }

        var current = parameters.SaturationCurrent * (exponential - 1.0);
        var conductance = parameters.SaturationCurrent * exponentialDerivative / voltageScale;
        var equivalentCurrent = current - (conductance * voltage);
        if (!Guard.IsFinite(current) || !Guard.IsFinite(conductance) || !Guard.IsFinite(equivalentCurrent))
        {
            throw new ArithmeticException("Diode evaluation produced a non-finite value.");
        }

        return new DiodeLinearization(current, conductance, equivalentCurrent);
    }
}

/// <summary>Contains a diode value and its affine tangent model.</summary>
public readonly record struct DiodeLinearization(double Current, double Conductance, double EquivalentCurrent);
