#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Components
{

/// <summary>Immutable parameters for a capacitor.</summary>
internal sealed record CapacitorParameters : IComponentParameters
{
    /// <summary>Initializes capacitor parameters.</summary>
    public CapacitorParameters(double capacitance)
    {
        if (!Guard.IsFinite(capacitance) || capacitance <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacitance), capacitance, "Capacitance must be finite and greater than zero.");
        }

        Capacitance = capacitance;
    }

    /// <summary>Gets capacitance in farads.</summary>
    public double Capacitance { get; }

    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.Capacitor;
}

}
