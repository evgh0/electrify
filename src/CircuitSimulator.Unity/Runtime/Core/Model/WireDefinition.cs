#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Model
{

/// <summary>
/// Defines an ideal zero-resistance wire between two physical terminals.
/// </summary>
/// <param name="First">The first terminal.</param>
/// <param name="Second">The second terminal.</param>
internal sealed record WireDefinition(TerminalId First, TerminalId Second);

}
