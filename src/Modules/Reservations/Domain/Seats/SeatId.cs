namespace TicketFlow.Reservations.Domain.Seats;

/// <summary>
/// Strongly typed unique identifier of a seat.
/// </summary>
/// <param name="Value">Underlying identifier value.</param>
public readonly record struct SeatId(Guid Value)
{
    /// <summary>
    /// Creates a new unique seat identifier.
    /// </summary>
    /// <returns>A new seat identifier.</returns>
    public static SeatId New() =>
        new(Guid.CreateVersion7());

    /// <summary>
    /// Gets an empty seat identifier.
    /// </summary>
    public static SeatId Empty =>
        new(Guid.Empty);

    /// <summary>
    /// Gets a value indicating whether the identifier is empty.
    /// </summary>
    public bool IsEmpty =>
        Value == Guid.Empty;

    /// <inheritdoc />
    public override string ToString() =>
        Value.ToString();
}
