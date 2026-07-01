using System.Collections.ObjectModel;

namespace CircuitSimulator.Core.Model;

/// <summary>
/// Routes component terminals through a solderless breadboard layout using ordinary ideal wires.
/// </summary>
/// <remarks>
/// A breadboard does not create component definitions or solver variables. It is an append-only helper tied
/// to one <see cref="CircuitBuilder"/>: inserting terminals into connected holes adds the same ideal wires
/// that a user could add manually with <see cref="CircuitBuilder.Connect(TerminalId, TerminalId)"/>.
/// </remarks>
public sealed class Breadboard
{
    private readonly CircuitBuilder _builder;
    private readonly Dictionary<BreadboardHole, TerminalId> _occupiedHoles = [];
    private readonly IReadOnlyDictionary<BreadboardHole, TerminalId> _occupiedHolesView;
    private readonly Dictionary<TerminalId, BreadboardHole> _terminalLocations = [];
    private readonly Dictionary<BreadboardNet, BreadboardNet> _parents = [];
    private readonly Dictionary<BreadboardNet, TerminalId> _representatives = [];

    /// <summary>
    /// Initializes a helper for the standard 30-column breadboard layout.
    /// </summary>
    /// <param name="builder">The circuit builder that will receive generated ideal wires.</param>
    public Breadboard(CircuitBuilder builder)
        : this(builder, BreadboardLayout.Standard)
    {
    }

    /// <summary>
    /// Initializes a helper for the supplied breadboard layout.
    /// </summary>
    /// <param name="builder">The circuit builder that will receive generated ideal wires.</param>
    /// <param name="layout">The immutable breadboard topology.</param>
    public Breadboard(CircuitBuilder builder, BreadboardLayout layout)
    {
        Guard.NotNull(builder, nameof(builder));
        Guard.NotNull(layout, nameof(layout));

        _builder = builder;
        Layout = layout;
        _occupiedHolesView = new ReadOnlyDictionary<BreadboardHole, TerminalId>(_occupiedHoles);
    }

    /// <summary>
    /// Gets the breadboard topology used by this helper.
    /// </summary>
    public BreadboardLayout Layout { get; }

    /// <summary>
    /// Gets the currently occupied holes and their inserted terminal identifiers.
    /// </summary>
    public IReadOnlyDictionary<BreadboardHole, TerminalId> OccupiedHoles => _occupiedHolesView;

    /// <summary>
    /// Inserts one component terminal into a breadboard hole.
    /// </summary>
    /// <param name="hole">The target breadboard hole.</param>
    /// <param name="terminal">The component terminal to insert.</param>
    /// <returns>The inserted terminal identifier.</returns>
    /// <exception cref="CircuitModelException">
    /// Thrown when the hole is already occupied, the terminal has already been inserted, or the terminal
    /// is not owned by the associated builder.
    /// </exception>
    public TerminalId Insert(BreadboardHole hole, TerminalId terminal)
    {
        var root = Find(Layout.GetNet(hole));

        if (_occupiedHoles.ContainsKey(hole))
        {
            throw new CircuitModelException($"Breadboard hole {hole} is already occupied.");
        }

        if (_terminalLocations.TryGetValue(terminal, out var existingHole))
        {
            throw new CircuitModelException($"Terminal {terminal} is already inserted into breadboard hole {existingHole}.");
        }

        if (_representatives.TryGetValue(root, out var representative))
        {
            _builder.Connect(representative, terminal);
        }
        else
        {
            ValidateTerminalOwnedByBuilder(terminal);
            _representatives.Add(root, terminal);
        }

        _occupiedHoles.Add(hole, terminal);
        _terminalLocations.Add(terminal, hole);
        return terminal;
    }

    /// <summary>
    /// Connects two breadboard nets with an ideal jumper.
    /// </summary>
    /// <param name="first">A hole on the first net.</param>
    /// <param name="second">A hole on the second net.</param>
    /// <remarks>
    /// This method joins whole breadboard nets. It does not mark the two holes as occupied, so it can be used
    /// as a logical wiring shortcut before or after component terminals are inserted.
    /// </remarks>
    public void ConnectNets(BreadboardHole first, BreadboardHole second)
    {
        var firstRoot = Find(Layout.GetNet(first));
        var secondRoot = Find(Layout.GetNet(second));
        if (firstRoot.Equals(secondRoot))
        {
            return;
        }

        var newRoot = firstRoot.SortKey <= secondRoot.SortKey ? firstRoot : secondRoot;
        var oldRoot = newRoot.Equals(firstRoot) ? secondRoot : firstRoot;

        var newHasRepresentative = _representatives.TryGetValue(newRoot, out var newRepresentative);
        var oldHasRepresentative = _representatives.TryGetValue(oldRoot, out var oldRepresentative);

        if (newHasRepresentative && oldHasRepresentative)
        {
            _builder.Connect(newRepresentative, oldRepresentative);
        }

        _parents[oldRoot] = newRoot;

        if (!newHasRepresentative && oldHasRepresentative)
        {
            _representatives[newRoot] = oldRepresentative;
        }

        _representatives.Remove(oldRoot);
    }

    /// <summary>
    /// Determines whether two holes are connected by the native layout plus any added net jumpers.
    /// </summary>
    /// <param name="first">The first hole coordinate.</param>
    /// <param name="second">The second hole coordinate.</param>
    /// <returns><see langword="true"/> when the holes currently belong to the same breadboard net.</returns>
    public bool AreConnected(BreadboardHole first, BreadboardHole second) =>
        Find(Layout.GetNet(first)).Equals(Find(Layout.GetNet(second)));

    /// <summary>
    /// Marks the connected net containing the supplied hole as ground.
    /// </summary>
    /// <param name="hole">A hole on the net to mark as ground.</param>
    /// <exception cref="CircuitModelException">Thrown when no component terminal has been inserted into the net.</exception>
    public void MarkNetAsGround(BreadboardHole hole)
    {
        var root = Find(Layout.GetNet(hole));
        if (!_representatives.TryGetValue(root, out var representative))
        {
            throw new CircuitModelException($"Cannot mark breadboard net {root} as ground because no terminal has been inserted into it.");
        }

        _builder.MarkAsGround(representative);
    }

    /// <summary>
    /// Gets the inserted terminal for an occupied hole.
    /// </summary>
    /// <param name="hole">The breadboard hole coordinate.</param>
    /// <returns>The inserted terminal identifier.</returns>
    /// <exception cref="CircuitModelException">Thrown when the hole is empty.</exception>
    public TerminalId GetInsertedTerminal(BreadboardHole hole) =>
        _occupiedHoles.TryGetValue(hole, out var terminal)
            ? terminal
            : throw new CircuitModelException($"Breadboard hole {hole} is empty.");

    /// <summary>
    /// Attempts to get the inserted terminal for a hole.
    /// </summary>
    /// <param name="hole">The breadboard hole coordinate.</param>
    /// <param name="terminal">The inserted terminal when the hole is occupied.</param>
    /// <returns><see langword="true"/> when the hole is occupied.</returns>
    public bool TryGetInsertedTerminal(BreadboardHole hole, out TerminalId terminal) =>
        _occupiedHoles.TryGetValue(hole, out terminal);

    private BreadboardNet Find(BreadboardNet net)
    {
        if (!_parents.TryGetValue(net, out var parent))
        {
            _parents.Add(net, net);
            return net;
        }

        if (parent.Equals(net))
        {
            return net;
        }

        var root = Find(parent);
        _parents[net] = root;
        return root;
    }

    private void ValidateTerminalOwnedByBuilder(TerminalId terminal)
    {
        _builder.Connect(terminal, terminal);
    }
}
