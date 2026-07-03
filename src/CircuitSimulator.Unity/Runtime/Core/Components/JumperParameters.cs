#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Components
{

/// <summary>Immutable parameters for a readable ideal jumper wire.</summary>
internal sealed record JumperParameters : IComponentParameters
{
    /// <inheritdoc />
    public ComponentKind Kind => ComponentKind.Jumper;
}

}
