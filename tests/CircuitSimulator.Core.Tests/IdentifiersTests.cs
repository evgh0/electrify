using CircuitSimulator.Core.Model;

namespace CircuitSimulator.Core.Tests;

public sealed class IdentifiersTests
{
    [Fact]
    public void ComponentIdsSupportEqualityHashingAndFormatting()
    {
        var first = new ComponentId(3);
        var second = new ComponentId(3);
        var other = new ComponentId(4);
        var values = new Dictionary<ComponentId, string> { [first] = "R1" };

        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
        Assert.Equal("R1", values[second]);
        Assert.Equal("ComponentId(3)", first.ToString());
    }

    [Fact]
    public void IdsRejectNegativeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ComponentId(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TerminalId(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NodeId(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VariableIndex(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BranchVariableId(-1));
    }
}
