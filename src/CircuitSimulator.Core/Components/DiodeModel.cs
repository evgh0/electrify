namespace CircuitSimulator.Core.Components;

/// <summary>Evaluates the Shockley diode model and its tangent linearization.</summary>
public static class DiodeModel
{
    private const double ExponentialContinuationThreshold = 40.0;

    /// <summary>Evaluates current, conductance, and equivalent-source current at a diode voltage.</summary>
    public static DiodeLinearization Evaluate(DiodeParameters parameters, double voltage)
    {
        Guard.NotNull(parameters, nameof(parameters));
        return Evaluate(
            parameters.SaturationCurrent,
            parameters.IdealityFactor,
            parameters.ThermalVoltage,
            voltage);
    }

    /// <summary>Evaluates current, conductance, and equivalent-source current at an LED voltage.</summary>
    public static DiodeLinearization Evaluate(LedParameters parameters, double voltage)
    {
        Guard.NotNull(parameters, nameof(parameters));
        return Evaluate(
            parameters.SaturationCurrent,
            parameters.IdealityFactor,
            parameters.ThermalVoltage,
            voltage);
    }

    private static DiodeLinearization Evaluate(
        double saturationCurrent,
        double idealityFactor,
        double thermalVoltage,
        double voltage)
    {
        if (!Guard.IsFinite(voltage))
        {
            throw new ArgumentOutOfRangeException(nameof(voltage), voltage, "Diode voltage must be finite.");
        }

        var voltageScale = idealityFactor * thermalVoltage;
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

        var current = saturationCurrent * (exponential - 1.0);
        var conductance = saturationCurrent * exponentialDerivative / voltageScale;
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
