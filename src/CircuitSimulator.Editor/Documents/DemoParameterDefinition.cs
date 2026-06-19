using System.Collections.ObjectModel;
using CircuitSimulator.Editor.Contracts.Documents;

namespace CircuitSimulator.Editor.Documents;

/// <summary>Describes one editable SI-valued demo parameter.</summary>
public sealed record DemoParameterDefinition(
    string Key,
    DemoComponentKey? Owner,
    string DisplayName,
    string Unit,
    double DefaultValue,
    double MinimumExclusive = double.NegativeInfinity,
    double MaximumInclusive = double.PositiveInfinity)
{
    /// <summary>Validates a candidate parameter value.</summary>
    public bool IsValid(double value) =>
        double.IsFinite(value) && value > MinimumExclusive && value <= MaximumInclusive;
}

/// <summary>Immutable validated values for a demo definition.</summary>
public sealed class DemoParameterValues
{
    private readonly IReadOnlyDictionary<string, double> _values;

    /// <summary>Initializes values from parameter defaults.</summary>
    public DemoParameterValues(IEnumerable<DemoParameterDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        _values = new ReadOnlyDictionary<string, double>(
            definitions.ToDictionary(definition => definition.Key, definition => definition.DefaultValue));
    }

    private DemoParameterValues(IReadOnlyDictionary<string, double> values) => _values = values;

    /// <summary>Gets a parameter value by stable key.</summary>
    public double this[string key] => _values[key];

    /// <summary>Returns a new snapshot with one validated replacement.</summary>
    public DemoParameterValues With(DemoParameterDefinition definition, double value)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!definition.IsValid(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Value is invalid for parameter '{definition.Key}'.");
        }

        var copy = new Dictionary<string, double>(_values)
        {
            [definition.Key] = value
        };
        return new DemoParameterValues(new ReadOnlyDictionary<string, double>(copy));
    }
}
