using System.Collections.ObjectModel;
using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Simulation;

/// <summary>Defines explicit reactive initial conditions for one transient run.</summary>
public sealed class TransientInitialConditions
{
    /// <summary>Gets an initial-condition set in which every unspecified value is zero.</summary>
    public static TransientInitialConditions Zero { get; } = new();

    /// <summary>Initializes an immutable initial-condition set.</summary>
    public TransientInitialConditions(
        IReadOnlyDictionary<ComponentId, double>? capacitorVoltages = null,
        IReadOnlyDictionary<ComponentId, double>? inductorCurrents = null)
    {
        CapacitorVoltages = CopyAndValidate(capacitorVoltages, nameof(capacitorVoltages));
        InductorCurrents = CopyAndValidate(inductorCurrents, nameof(inductorCurrents));
    }

    /// <summary>Gets initial capacitor voltages, positive terminal minus negative terminal.</summary>
    public IReadOnlyDictionary<ComponentId, double> CapacitorVoltages { get; }

    /// <summary>Gets initial inductor currents, positive terminal to negative terminal.</summary>
    public IReadOnlyDictionary<ComponentId, double> InductorCurrents { get; }

    private static IReadOnlyDictionary<ComponentId, double> CopyAndValidate(
        IReadOnlyDictionary<ComponentId, double>? source,
        string parameterName)
    {
        var copy = source is null
            ? new Dictionary<ComponentId, double>()
            : new Dictionary<ComponentId, double>(source);

        foreach (var pair in copy)
        {
            if (!double.IsFinite(pair.Value))
            {
                throw new ArgumentException($"Initial condition for {pair.Key} must be finite.", parameterName);
            }
        }

        return new ReadOnlyDictionary<ComponentId, double>(copy);
    }
}
