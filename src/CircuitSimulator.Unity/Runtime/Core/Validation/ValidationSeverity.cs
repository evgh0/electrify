#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Validation
{

/// <summary>
/// Describes the severity of a circuit validation issue.
/// </summary>
internal enum ValidationSeverity
{
    /// <summary>
    /// Informational validation output.
    /// </summary>
    Info,

    /// <summary>
    /// A non-fatal validation warning.
    /// </summary>
    Warning,

    /// <summary>
    /// A fatal validation error.
    /// </summary>
    Error
}

}
