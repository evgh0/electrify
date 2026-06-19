using CircuitSimulator.Editor.Documents;
using CircuitSimulator.Editor.Properties;
using CircuitSimulator.Editor.ViewModels;

namespace CircuitSimulator.Editor.Tests;

public sealed class EngineeringNotationTests
{
    [Theory]
    [InlineData("4.7k", 4700.0)]
    [InlineData("100u", 0.0001)]
    [InlineData("10m", 0.01)]
    [InlineData("1Meg", 1_000_000.0)]
    public void ParsesEngineeringValues(string text, double expected)
    {
        Assert.True(EngineeringNotation.TryParse(text, out var actual));
        Assert.Equal(expected, actual, 10);
    }

    [Fact]
    public void InvalidFieldRetainsTextAndReportsInlineError()
    {
        var definition = new DemoParameterDefinition("r", null, "Resistance", "Ω", 1000.0, 0.0);
        var field = new ParameterFieldViewModel(definition, definition.DefaultValue)
        {
            Text = "not-a-number"
        };

        Assert.False(field.TryGetValue(out _));
        Assert.True(field.HasError);
        Assert.Equal("not-a-number", field.Text);
    }

    [Fact]
    public void ParameterReplacementCreatesAnImmutableSnapshot()
    {
        var definition = new DemoParameterDefinition("r", null, "Resistance", "Ω", 1000.0, 0.0);
        var original = new DemoParameterValues([definition]);

        var changed = original.With(definition, 4700.0);

        Assert.Equal(1000.0, original[definition.Key]);
        Assert.Equal(4700.0, changed[definition.Key]);
        Assert.Throws<ArgumentOutOfRangeException>(() => original.With(definition, 0.0));
    }
}
