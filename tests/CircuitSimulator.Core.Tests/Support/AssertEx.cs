namespace CircuitSimulator.Core.Tests.Support;

internal static class AssertEx
{
    public static void NearlyEqual(double expected, double actual, double tolerance = 1e-9)
    {
        var scale = Math.Max(1.0, Math.Max(Math.Abs(expected), Math.Abs(actual)));
        Assert.True(
            Math.Abs(expected - actual) <= tolerance * scale,
            $"Expected {expected:R}, actual {actual:R}, tolerance {tolerance:R}.");
    }
}
