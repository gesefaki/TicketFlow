namespace TicketFlow.Reservations.Domain.Customers;

/// <summary>
/// Strongly typed unique identifier of a customer.
/// </summary>
/// <param name="Value">Underlying identifier value.</param>
public readonly record struct CustomerId(Guid Value)
{
    /// <summary>
    /// Creates a new unique customer identifier.
    /// </summary>
    /// <returns>A new customer identifier.</returns>
    public static CustomerId New() =>
        new(Guid.CreateVersion7());

    /// <summary>
    /// Gets an empty customer identifier.
    /// </summary>
    public static CustomerId Empty =>
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
