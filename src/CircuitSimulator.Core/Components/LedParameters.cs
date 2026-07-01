namespace CircuitSimulator.Core.Components;

/// <summary>Immutable Shockley-equivalent LED parameters.</summary>
public sealed record LedParameters : IComponentParameters
{
    /// <summary>Default nominal forward voltage for a red indicator LED.</summary>
    public const double DefaultNominalForwardVoltage = 2.0;

    /// <summary>Default reference forward current for a small indicator LED.</summary>
    public const double DefaultReferenceCurrent = 0.02;

    /// <summary>Default LED emission ideality factor.</summary>
    public const double DefaultIdealityFactor = 2.0;

    /// <summary>Initializes LED parameters from a nominal forward-voltage operating point.</summary>
    public LedParameters(
        double nominalForwardVoltage = DefaultNominalForwardVoltage,
        double referenceCurrent = DefaultReferenceCurrent,
        double idealityFactor = DefaultIdealityFactor,
        double thermalVoltage = DiodeParameters.DefaultThermalVoltage)
    {
        if (!Guard.IsFinite(nominalForwardVoltage) || nominalForwardVoltage <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nominalForwardVoltage),
                nominalForwardVoltage,
                "Nominal forward voltage must be finite and greater than zero.");
        }

        if (!Guard.IsFinite(referenceCurrent) || referenceCurrent <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(referenceCurrent),
                referenceCurrent,
                "Reference current must be finite and greater than zero.");
        }

        if (!Guard.IsFinite(idealityFactor) || idealityFactor <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idealityFactor),
                idealityFactor,
                "Ideality factor must be finite and greater than zero.");
        }

        if (!Guard.IsFinite(thermalVoltage) || thermalVoltage <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(thermalVoltage),
                thermalVoltage,
                "Thermal voltage must be finite and greater than zero.");
        }

        var saturationCurrent = CalculateSaturationCurrent(
            nominalForwardVoltage,
            referenceCurrent,
            idealityFactor,
            thermalVoltage);
        if (!Guard.IsFinite(saturationCurrent) || saturationCurrent <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nominalForwardVoltage),
                nominalForwardVoltage,
                "LED parameters derive a non-finite or non-positive saturation current.");
        }

        NominalForwardVoltage = nominalForwardVoltage;
        ReferenceCurrent = referenceCurrent;
        IdealityFactor = idealityFactor;
        ThermalVoltage = thermalVoltage;
        SaturationCurrent = saturationCurrent;
    }

    /// <summary>Gets nominal forward voltage in volts.</summary>
    public double NominalForwardVoltage { get; }

    /// <summary>Gets reference forward current in amperes.</summary>
    public double ReferenceCurrent { get; }

    /// <summary>Gets the emission ideality factor.</summary>
    public double IdealityFactor { get; }

    /// <summary>Gets thermal voltage in volts.</summary>
    public double ThermalVoltage { get; }

    /// <summary>Gets the derived reverse saturation current in amperes.</summary>
    public double SaturationCurrent { get; }

    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.Led;

    private static double CalculateSaturationCurrent(
        double nominalForwardVoltage,
        double referenceCurrent,
        double idealityFactor,
        double thermalVoltage)
    {
        var exponent = nominalForwardVoltage / (idealityFactor * thermalVoltage);
        var denominator = Math.Exp(exponent) - 1.0;
        return referenceCurrent / denominator;
    }
}
