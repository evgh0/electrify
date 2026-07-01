#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Mna
{

/// <summary>
/// Describes the kind of an allocated MNA unknown.
/// </summary>
internal enum MnaVariableKind
{
    /// <summary>
    /// A non-ground node voltage unknown.
    /// </summary>
    NodeVoltage,

    /// <summary>
    /// A branch-current unknown for an ideal voltage source.
    /// </summary>
    BranchCurrent
}

}
