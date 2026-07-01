#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Mna
{

/// <summary>
/// Represents a failure while assembling an MNA linear system.
/// </summary>
internal class MnaAssemblyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MnaAssemblyException"/> class.
    /// </summary>
    public MnaAssemblyException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MnaAssemblyException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public MnaAssemblyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MnaAssemblyException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public MnaAssemblyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

}
