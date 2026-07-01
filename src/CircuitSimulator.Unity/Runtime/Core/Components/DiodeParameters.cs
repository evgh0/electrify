#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Components
{

/// <summary>Immutable Shockley diode parameters.</summary>
internal sealed record DiodeParameters : IComponentParameters
{
    /// <summary>Default thermal voltage at approximately 300 K.</summary>
    public const double DefaultThermalVoltage = 0.025851999786;

    /// <summary>Initializes diode parameters.</summary>
    public DiodeParameters(
        double saturationCurrent = 1e-12,
        double idealityFactor = 1.0,
        double thermalVoltage = DefaultThermalVoltage)
    {
        if (!Guard.IsFinite(saturationCurrent) || saturationCurrent <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(saturationCurrent), saturationCurrent, "Saturation current must be finite and greater than zero.");
        }

        if (!Guard.IsFinite(idealityFactor) || idealityFactor <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(idealityFactor), idealityFactor, "Ideality factor must be finite and greater than zero.");
        }

        if (!Guard.IsFinite(thermalVoltage) || thermalVoltage <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(thermalVoltage), thermalVoltage, "Thermal voltage must be finite and greater than zero.");
        }

        SaturationCurrent = saturationCurrent;
        IdealityFactor = idealityFactor;
        ThermalVoltage = thermalVoltage;
    }

    /// <summary>Gets reverse saturation current in amperes.</summary>
    public double SaturationCurrent { get; }

    /// <summary>Gets the emission ideality factor.</summary>
    public double IdealityFactor { get; }

    /// <summary>Gets thermal voltage in volts.</summary>
    public double ThermalVoltage { get; }

    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.Diode;
}

}
