namespace TicketFlow.Reservations.Domain.Reservations;

/// <summary>
/// Strongly typed unique identifier of a reservation.
/// </summary>
/// <param name="Value">Underlying identifier value.</param>
public readonly record struct ReservationId(Guid Value)
{
    /// <summary>
    /// Creates a new unique reservation identifier.
    /// </summary>
    /// <returns>A new reservation identifier.</returns>
    public static ReservationId New() =>
        new(Guid.CreateVersion7());

    /// <summary>
    /// Gets an empty reservation identifier.
    /// </summary>
    public static ReservationId Empty =>
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
