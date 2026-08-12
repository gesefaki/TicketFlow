using TicketFlow.BuildingBlocks.Domain.Interfaces;
using TicketFlow.Reservations.Domain.Reservations;

namespace TicketFlow.Reservations.Domain.Events;

/// <summary>
/// Indicates that a reservation has expired.
/// </summary>
/// <param name="ReservationId">Identifier of the expired reservation.</param>
/// <param name="ExpiredAt">Timestamp at which expiration was processed.</param>
public sealed record ReservationExpiredDomainEvent(
    ReservationId ReservationId,
    DateTimeOffset ExpiredAt)
    : IDomainEvent;
