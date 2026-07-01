#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core.Topology
{

internal sealed class DisjointSet
{
    private readonly int[] _parent;
    private readonly int[] _rank;

    public DisjointSet(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Disjoint-set size cannot be negative.");
        }

        _parent = new int[count];
        _rank = new int[count];

        for (var index = 0; index < count; index++)
        {
            _parent[index] = index;
        }
    }

    public int Find(int item)
    {
        ValidateIndex(item);

        if (_parent[item] != item)
        {
            _parent[item] = Find(_parent[item]);
        }

        return _parent[item];
    }

    public void Union(int first, int second)
    {
        var firstRoot = Find(first);
        var secondRoot = Find(second);

        if (firstRoot == secondRoot)
        {
            return;
        }

        if (_rank[firstRoot] < _rank[secondRoot])
        {
            _parent[firstRoot] = secondRoot;
        }
        else if (_rank[firstRoot] > _rank[secondRoot])
        {
            _parent[secondRoot] = firstRoot;
        }
        else
        {
            _parent[secondRoot] = firstRoot;
            _rank[firstRoot]++;
        }
    }

    private void ValidateIndex(int item)
    {
        if (item < 0 || item >= _parent.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(item), item, "Disjoint-set index is outside the valid range.");
        }
    }
}

}
