namespace CircuitSimulator.Core.Model;

/// <summary>
/// Defines one physical terminal owned by a component.
/// </summary>
public sealed class TerminalDefinition
{
    /// <summary>
    /// Initializes a new terminal definition.
    /// </summary>
    /// <param name="id">The terminal identifier.</param>
    /// <param name="ownerComponentId">The owning component identifier.</param>
    /// <param name="localIndex">The zero-based local terminal index.</param>
    /// <param name="name">The terminal name.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="localIndex"/> is negative.</exception>
    public TerminalDefinition(TerminalId id, ComponentId ownerComponentId, int localIndex, string name)
    {
        Guard.NotNull(name, nameof(name));

        if (localIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(localIndex), localIndex, "Terminal local index cannot be negative.");
        }

        Id = id;
        OwnerComponentId = ownerComponentId;
        LocalIndex = localIndex;
        Name = name;
    }

    /// <summary>
    /// Gets the terminal identifier.
    /// </summary>
    public TerminalId Id { get; }

    /// <summary>
    /// Gets the component that owns this terminal.
    /// </summary>
    public ComponentId OwnerComponentId { get; }

    /// <summary>
    /// Gets the terminal index within the owner component.
    /// </summary>
    public int LocalIndex { get; }

    /// <summary>
    /// Gets the terminal display name.
    /// </summary>
    public string Name { get; }
}
