#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CircuitSimulator.Core.Compilation;

namespace CircuitSimulator.Core.Mna
{

/// <summary>
/// Provides immutable topology and variable mapping information to component stamps.
/// </summary>
internal sealed class MnaStampContext
{
    /// <summary>
    /// Initializes a stamp context.
    /// </summary>
    /// <param name="circuit">The compiled circuit.</param>
    public MnaStampContext(CompiledCircuit circuit)
    {
        Circuit = circuit ?? throw new ArgumentNullException(nameof(circuit));
        Variables = circuit.VariableMap;
    }

    /// <summary>
    /// Gets the compiled circuit.
    /// </summary>
    public CompiledCircuit Circuit { get; }

    /// <summary>
    /// Gets the MNA variable map.
    /// </summary>
    public MnaVariableMap Variables { get; }
}

}
