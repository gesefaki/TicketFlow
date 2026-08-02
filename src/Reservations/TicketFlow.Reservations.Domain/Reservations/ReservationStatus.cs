namespace TicketFlow.Reservations.Domain.Reservations;

/// <summary>
/// Represents the current state of a reservation.
/// </summary>
public enum ReservationStatus
{
    /// <summary>
    /// The reservation is active and awaiting confirmation.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// The reservation has been confirmed.
    /// </summary>
    Confirmed = 2,

    /// <summary>
    /// The reservation has been canceled.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// The reservation has expired.
    /// </summary>
    Expired = 4
}
