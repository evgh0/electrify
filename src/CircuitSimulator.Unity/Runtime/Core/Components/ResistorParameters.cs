#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Components
{

/// <summary>
/// Immutable parameters for a resistor.
/// </summary>
internal sealed record ResistorParameters : IComponentParameters
{
    /// <summary>
    /// Gets the resistance in ohms.
    /// </summary>
    public double Resistance { get; }

    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.Resistor;

    /// <summary>
    /// Initializes a new resistor parameter set.
    /// </summary>
    /// <param name="resistance">The finite, strictly positive resistance in ohms.</param>
    public ResistorParameters(double resistance)
    {
        if (!Guard.IsFinite(resistance) || resistance <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(resistance), resistance, "Resistance must be finite and greater than zero.");
        }

        Resistance = resistance;
    }
}

}
