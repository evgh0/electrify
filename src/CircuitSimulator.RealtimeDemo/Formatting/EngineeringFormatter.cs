using System.Globalization;

namespace CircuitSimulator.RealtimeDemo.Formatting;

internal static class EngineeringFormatter
{
    private static readonly (double Scale, string Prefix)[] Prefixes =
    [
        (1e9, "G"),
        (1e6, "M"),
        (1e3, "k"),
        (1.0, string.Empty),
        (1e-3, "m"),
        (1e-6, "µ"),
        (1e-9, "n"),
        (1e-12, "p")
    ];

    public static string Format(double value, string unit)
    {
        if (!double.IsFinite(value))
        {
            return $"{value.ToString(CultureInfo.InvariantCulture)} {unit}";
        }

        if (value == 0.0)
        {
            return $"0 {unit}";
        }

        var magnitude = Math.Abs(value);
        var prefix = Prefixes.FirstOrDefault(item => magnitude >= item.Scale);
        if (prefix.Scale == 0.0)
        {
            prefix = Prefixes[^1];
        }

        var scaled = value / prefix.Scale;
        return $"{scaled.ToString("0.###", CultureInfo.InvariantCulture)} {prefix.Prefix}{unit}";
    }
}
