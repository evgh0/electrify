#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Components
{

/// <summary>
/// Describes immutable parameters for a component definition.
/// </summary>
internal interface IComponentParameters
{
    /// <summary>
    /// Gets the component kind represented by these parameters.
    /// </summary>
    ComponentKind Kind { get; }
}

}
