using System.Globalization;
using System.Text.RegularExpressions;

namespace CircuitSimulator.Editor.Properties;

/// <summary>Parses and formats finite SI values using engineering prefixes.</summary>
public static partial class EngineeringNotation
{
    /// <summary>Attempts to parse an invariant number with an optional engineering prefix.</summary>
    public static bool TryParse(string? text, out double value)
    {
        value = 0.0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var match = ValuePattern().Match(text.Trim());
        if (!match.Success ||
            !double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        var multiplier = match.Groups[2].Value switch
        {
            "" => 1.0,
            "p" => 1e-12,
            "n" => 1e-9,
            "u" or "µ" or "μ" => 1e-6,
            "m" => 1e-3,
            "k" or "K" => 1e3,
            "M" or "Meg" or "meg" => 1e6,
            "G" or "g" => 1e9,
            _ => double.NaN
        };

        value = number * multiplier;
        return double.IsFinite(value);
    }

    /// <summary>Formats a finite value with a compact engineering prefix.</summary>
    public static string Format(double value, string unit = "")
    {
        if (!double.IsFinite(value))
        {
            return "—";
        }

        var magnitude = Math.Abs(value);
        var (scale, prefix) = magnitude switch
        {
            >= 1e9 => (1e9, "G"),
            >= 1e6 => (1e6, "M"),
            >= 1e3 => (1e3, "k"),
            >= 1.0 => (1.0, ""),
            >= 1e-3 => (1e-3, "m"),
            >= 1e-6 => (1e-6, "µ"),
            >= 1e-9 => (1e-9, "n"),
            > 0.0 => (1e-12, "p"),
            _ => (1.0, "")
        };

        var suffix = string.IsNullOrEmpty(unit) ? prefix : $"{prefix}{unit}";
        return $"{value / scale:0.###}{suffix}";
    }

    [GeneratedRegex(@"^([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)\s*(Meg|meg|[pnuµμmkKMGg]?)$")]
    private static partial Regex ValuePattern();
}
