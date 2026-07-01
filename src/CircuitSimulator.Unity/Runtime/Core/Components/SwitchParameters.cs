#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Components
{

/// <summary>Immutable parameters for a controllable ideal switch.</summary>
internal sealed record SwitchParameters : IComponentParameters
{
    /// <summary>Initializes ideal-switch parameters.</summary>
    /// <param name="initiallyClosed">
    /// <see langword="true"/> to start as an ideal short; <see langword="false"/> to start as an open circuit.
    /// </param>
    public SwitchParameters(bool initiallyClosed = false)
    {
        InitiallyClosed = initiallyClosed;
    }

    /// <summary>Gets whether a new simulation run starts with the switch closed.</summary>
    public bool InitiallyClosed { get; }

    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.Switch;
}

}
