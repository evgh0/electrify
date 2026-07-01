namespace CircuitSimulator.Core.Model;

/// <summary>
/// Identifies a terminal-strip row on a solderless breadboard.
/// </summary>
public enum BreadboardRow
{
    /// <summary>Row A, connected vertically with rows B through E in the same column.</summary>
    A,

    /// <summary>Row B, connected vertically with rows A and C through E in the same column.</summary>
    B,

    /// <summary>Row C, connected vertically with rows A, B, D, and E in the same column.</summary>
    C,

    /// <summary>Row D, connected vertically with rows A through C and E in the same column.</summary>
    D,

    /// <summary>Row E, connected vertically with rows A through D in the same column.</summary>
    E,

    /// <summary>Row F, connected vertically with rows G through J in the same column.</summary>
    F,

    /// <summary>Row G, connected vertically with rows F and H through J in the same column.</summary>
    G,

    /// <summary>Row H, connected vertically with rows F, G, I, and J in the same column.</summary>
    H,

    /// <summary>Row I, connected vertically with rows F through H and J in the same column.</summary>
    I,

    /// <summary>Row J, connected vertically with rows F through I in the same column.</summary>
    J
}

/// <summary>
/// Identifies one continuous red or blue power rail on the breadboard layout.
/// </summary>
public enum BreadboardPowerRail
{
    /// <summary>The upper blue negative rail.</summary>
    TopNegative,

    /// <summary>The upper red positive rail.</summary>
    TopPositive,

    /// <summary>The lower blue negative rail.</summary>
    BottomNegative,

    /// <summary>The lower red positive rail.</summary>
    BottomPositive
}

/// <summary>
/// Identifies whether a breadboard coordinate is in the terminal strips or a power rail.
/// </summary>
public enum BreadboardHoleKind
{
    /// <summary>A hole in rows A through J.</summary>
    TerminalStrip,

    /// <summary>A hole in a red or blue power rail.</summary>
    PowerRail
}

/// <summary>
/// Identifies the native net group represented by a breadboard hole.
/// </summary>
public enum BreadboardNetKind
{
    /// <summary>Rows A through E in one numbered column.</summary>
    TerminalStripAThroughE,

    /// <summary>Rows F through J in one numbered column.</summary>
    TerminalStripFThroughJ,

    /// <summary>One continuous red or blue power rail.</summary>
    PowerRail
}

/// <summary>
/// Identifies one addressable hole in the standard breadboard layout.
/// </summary>
public readonly record struct BreadboardHole
{
    private BreadboardHole(BreadboardHoleKind kind, BreadboardRow? row, BreadboardPowerRail? rail, int column)
    {
        Kind = kind;
        Row = row;
        Rail = rail;
        Column = column;
    }

    /// <summary>
    /// Gets whether this coordinate is a terminal-strip hole or a power-rail hole.
    /// </summary>
    public BreadboardHoleKind Kind { get; }

    /// <summary>
    /// Gets the terminal-strip row for terminal-strip holes.
    /// </summary>
    public BreadboardRow? Row { get; }

    /// <summary>
    /// Gets the red or blue rail for power-rail holes.
    /// </summary>
    public BreadboardPowerRail? Rail { get; }

    /// <summary>
    /// Gets the one-based column number.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// Creates a coordinate for one hole in rows A through J.
    /// </summary>
    /// <param name="row">The terminal-strip row.</param>
    /// <param name="column">The one-based column number.</param>
    /// <returns>The breadboard hole coordinate.</returns>
    public static BreadboardHole TerminalStrip(BreadboardRow row, int column)
    {
        ValidateEnum(row, nameof(row));
        ValidateColumn(column);
        return new BreadboardHole(BreadboardHoleKind.TerminalStrip, row, null, column);
    }

    /// <summary>
    /// Creates a coordinate for one hole on a red or blue power rail.
    /// </summary>
    /// <param name="rail">The power rail.</param>
    /// <param name="column">The one-based column number.</param>
    /// <returns>The breadboard hole coordinate.</returns>
    public static BreadboardHole PowerRail(BreadboardPowerRail rail, int column)
    {
        ValidateEnum(rail, nameof(rail));
        ValidateColumn(column);
        return new BreadboardHole(BreadboardHoleKind.PowerRail, null, rail, column);
    }

    /// <inheritdoc />
    public override string ToString() =>
        Kind == BreadboardHoleKind.TerminalStrip
            ? $"{Row}{Column}"
            : $"{Rail}[{Column}]";

    private static void ValidateColumn(int column)
    {
        if (column < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(column), column, "Breadboard columns are one-based.");
        }
    }

    private static void ValidateEnum<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Breadboard coordinate enum value is not defined.");
        }
    }
}

/// <summary>
/// Identifies one native breadboard net before optional jumpers are applied.
/// </summary>
public readonly struct BreadboardNet : IEquatable<BreadboardNet>
{
    private readonly int _sortKey;

    internal BreadboardNet(
        BreadboardNetKind kind,
        int sortKey,
        int? column,
        BreadboardPowerRail? rail)
    {
        Kind = kind;
        _sortKey = sortKey;
        Column = column;
        Rail = rail;
    }

    /// <summary>
    /// Gets the net category.
    /// </summary>
    public BreadboardNetKind Kind { get; }

    /// <summary>
    /// Gets the one-based column for terminal-strip nets, or <see langword="null"/> for power rails.
    /// </summary>
    public int? Column { get; }

    /// <summary>
    /// Gets the rail for power-rail nets, or <see langword="null"/> for terminal strips.
    /// </summary>
    public BreadboardPowerRail? Rail { get; }

    internal int SortKey => _sortKey;

    /// <inheritdoc />
    public bool Equals(BreadboardNet other) =>
        Kind == other.Kind &&
        _sortKey == other._sortKey &&
        Column == other.Column &&
        Rail == other.Rail;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BreadboardNet other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Kind, _sortKey, Column, Rail);

    /// <inheritdoc />
    public override string ToString() =>
        Kind switch
        {
            BreadboardNetKind.TerminalStripAThroughE => $"A-E column {Column}",
            BreadboardNetKind.TerminalStripFThroughJ => $"F-J column {Column}",
            BreadboardNetKind.PowerRail => $"{Rail}",
            _ => Kind.ToString()
        };
}
