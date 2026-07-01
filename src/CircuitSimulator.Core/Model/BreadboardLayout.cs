using System.Collections.ObjectModel;

namespace CircuitSimulator.Core.Model;

/// <summary>
/// Describes the native ideal-wire topology of a solderless breadboard.
/// </summary>
/// <remarks>
/// The standard layout has 30 numbered columns. Rows A through E in the same column are connected,
/// rows F through J in the same column are connected, and each red or blue power rail is continuous
/// across all columns. The upper and lower rails are not connected to each other by default.
/// </remarks>
public sealed class BreadboardLayout
{
    private static readonly BreadboardRow[] AThroughERows =
    [
        BreadboardRow.A,
        BreadboardRow.B,
        BreadboardRow.C,
        BreadboardRow.D,
        BreadboardRow.E
    ];

    private static readonly BreadboardRow[] FThroughJRows =
    [
        BreadboardRow.F,
        BreadboardRow.G,
        BreadboardRow.H,
        BreadboardRow.I,
        BreadboardRow.J
    ];

    /// <summary>
    /// Gets the default number of numbered terminal-strip columns.
    /// </summary>
    public const int StandardColumnCount = 30;

    /// <summary>
    /// Gets the standard 30-column breadboard layout.
    /// </summary>
    public static BreadboardLayout Standard { get; } = new(StandardColumnCount);

    /// <summary>
    /// Initializes a new breadboard layout.
    /// </summary>
    /// <param name="columnCount">The number of one-based columns.</param>
    public BreadboardLayout(int columnCount = StandardColumnCount)
    {
        if (columnCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(columnCount), columnCount, "A breadboard layout must have at least one column.");
        }

        ColumnCount = columnCount;
    }

    /// <summary>
    /// Gets the number of one-based columns in the layout.
    /// </summary>
    public int ColumnCount { get; }

    /// <summary>
    /// Gets the native ideal-wire net for a breadboard hole.
    /// </summary>
    /// <param name="hole">The breadboard hole coordinate.</param>
    /// <returns>The native breadboard net.</returns>
    public BreadboardNet GetNet(BreadboardHole hole)
    {
        EnsureColumnInRange(hole);

        return hole.Kind switch
        {
            BreadboardHoleKind.TerminalStrip => GetTerminalStripNet(hole),
            BreadboardHoleKind.PowerRail => GetPowerRailNet(hole),
            _ => throw new ArgumentOutOfRangeException(nameof(hole), hole, "Breadboard hole kind is not defined.")
        };
    }

    /// <summary>
    /// Determines whether two holes share the same native breadboard connection.
    /// </summary>
    /// <param name="first">The first hole coordinate.</param>
    /// <param name="second">The second hole coordinate.</param>
    /// <returns><see langword="true"/> when the holes are internally connected by the board layout.</returns>
    public bool AreConnected(BreadboardHole first, BreadboardHole second) =>
        GetNet(first).Equals(GetNet(second));

    /// <summary>
    /// Gets all holes connected to the same native breadboard net as the supplied hole.
    /// </summary>
    /// <param name="hole">The breadboard hole coordinate.</param>
    /// <returns>The connected hole coordinates in deterministic order.</returns>
    public IReadOnlyList<BreadboardHole> GetConnectedHoles(BreadboardHole hole)
    {
        var net = GetNet(hole);
        BreadboardHole[] holes = net.Kind switch
        {
            BreadboardNetKind.TerminalStripAThroughE => AThroughERows
                .Select(row => BreadboardHole.TerminalStrip(row, net.Column!.Value))
                .ToArray(),
            BreadboardNetKind.TerminalStripFThroughJ => FThroughJRows
                .Select(row => BreadboardHole.TerminalStrip(row, net.Column!.Value))
                .ToArray(),
            BreadboardNetKind.PowerRail => Enumerable
                .Range(1, ColumnCount)
                .Select(column => BreadboardHole.PowerRail(net.Rail!.Value, column))
                .ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(hole), hole, "Breadboard net kind is not defined.")
        };

        return new ReadOnlyCollection<BreadboardHole>(holes);
    }

    private BreadboardNet GetTerminalStripNet(BreadboardHole hole)
    {
        if (!hole.Row.HasValue || !Enum.IsDefined(typeof(BreadboardRow), hole.Row.Value))
        {
            throw new ArgumentException("Terminal-strip breadboard holes require a defined row.", nameof(hole));
        }

        var row = hole.Row.Value;
        if (row <= BreadboardRow.E)
        {
            return new BreadboardNet(
                BreadboardNetKind.TerminalStripAThroughE,
                hole.Column - 1,
                hole.Column,
                null);
        }

        return new BreadboardNet(
            BreadboardNetKind.TerminalStripFThroughJ,
            ColumnCount + hole.Column - 1,
            hole.Column,
            null);
    }

    private BreadboardNet GetPowerRailNet(BreadboardHole hole)
    {
        if (!hole.Rail.HasValue || !Enum.IsDefined(typeof(BreadboardPowerRail), hole.Rail.Value))
        {
            throw new ArgumentException("Power-rail breadboard holes require a defined rail.", nameof(hole));
        }

        return new BreadboardNet(
            BreadboardNetKind.PowerRail,
            (2 * ColumnCount) + (int)hole.Rail.Value,
            null,
            hole.Rail.Value);
    }

    private void EnsureColumnInRange(BreadboardHole hole)
    {
        if (hole.Column < 1 || hole.Column > ColumnCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hole),
                hole,
                $"Breadboard column must be between 1 and {ColumnCount}.");
        }
    }
}
