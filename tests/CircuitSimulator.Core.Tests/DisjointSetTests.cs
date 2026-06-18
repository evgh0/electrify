using CircuitSimulator.Core.Topology;

namespace CircuitSimulator.Core.Tests;

public sealed class DisjointSetTests
{
    [Fact]
    public void UnionFindKeepsSetsAndTransitiveConnectionsCorrect()
    {
        var disjointSet = new DisjointSet(5);

        Assert.NotEqual(disjointSet.Find(0), disjointSet.Find(1));

        disjointSet.Union(0, 1);
        disjointSet.Union(1, 2);
        disjointSet.Union(3, 4);
        disjointSet.Union(0, 2);

        Assert.Equal(disjointSet.Find(0), disjointSet.Find(2));
        Assert.Equal(disjointSet.Find(3), disjointSet.Find(4));
        Assert.NotEqual(disjointSet.Find(0), disjointSet.Find(3));
    }

    [Fact]
    public void RejectsInvalidIndexes()
    {
        var disjointSet = new DisjointSet(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DisjointSet(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => disjointSet.Find(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => disjointSet.Find(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => disjointSet.Union(0, 1));
    }
}
