#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Components
{

/// <summary>Immutable parameters for an inductor.</summary>
internal sealed record InductorParameters : IComponentParameters
{
    /// <summary>Initializes inductor parameters.</summary>
    public InductorParameters(double inductance)
    {
        if (!Guard.IsFinite(inductance) || inductance <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(inductance), inductance, "Inductance must be finite and greater than zero.");
        }

        Inductance = inductance;
    }

    /// <summary>Gets inductance in henries.</summary>
    public double Inductance { get; }

    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.Inductor;
}

}
