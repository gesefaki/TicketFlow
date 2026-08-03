using TicketFlow.BuildingBlocks.Domain.Common;
using TicketFlow.BuildingBlocks.Domain.Models;
using TicketFlow.Reservations.Domain.Customers;
using TicketFlow.Reservations.Domain.Seats;

namespace TicketFlow.Reservations.Domain.Reservations;

/// <summary>
/// Represents a time-limited reservation of a seat by a customer.
/// Controls the reservation lifecycle and enforces valid state transitions.
/// </summary>
public sealed class Reservation : AggregateRoot<ReservationId>
{
    private readonly List<SeatId> _seatIds = [];

    /// <summary>
    /// Read-only collection of identifiers of the reserved seats.
    /// </summary>
    public IReadOnlyCollection<SeatId> SeatIds => _seatIds.AsReadOnly();

    /// <summary>
    /// Identifier of the customer who owns the reservation.
    /// </summary>
    public CustomerId CustomerId { get; private set; }

    /// <summary>
    /// Confirmation timestamp, or <see langword="null"/> when the reservation is not confirmed.
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; private set; }

    /// <summary>
    /// Cancellation timestamp, or <see langword="null"/> when the reservation is not canceled.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>
    /// Timestamp at which the reservation ceases to be active.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private init; }

    /// <summary>
    /// Expiration processing timestamp, or <see langword="null"/> when the reservation is not expired.
    /// </summary>
    public DateTimeOffset? ExpiredAt { get; private set; }

    /// <summary>
    /// Represents the current state of a reservation.
    /// </summary>
    public ReservationStatus Status { get; private set; }
    
    private Reservation()
    {
    }

    /// <summary>
    /// Creates a new pending reservation for one or more seats and a customer.
    /// </summary>
    /// <param name="seatIds">Identifiers of the seats to reserve.</param>
    /// <param name="customerId">Identifier of the customer making the reservation.</param>
    /// <param name="reservedAt">Timestamp at which the reservation is created.</param>
    /// <param name="reservationDuration">Duration for which the reservation remains active.</param>
    /// <returns>A new pending reservation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the seat identifiers collection is <see langword="null"/>.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when the collection is empty, contains an empty or duplicate seat identifier,
    /// the customer identifier is empty, or the reservation duration is not positive.
    /// </exception>
    public static Reservation Reserve(
        IEnumerable<SeatId> seatIds,
        CustomerId customerId,
        DateTimeOffset reservedAt,
        TimeSpan reservationDuration)
    {
        ArgumentNullException.ThrowIfNull(seatIds);

        var reservedSeatIds = seatIds.ToList();

        if (reservedSeatIds.Count == 0)
        {
            throw new DomainException($"{nameof(Reservation)}: {nameof(seatIds)} is empty.");
        }

        if (reservedSeatIds.Any(seatId => seatId.IsEmpty))
        {
            throw new DomainException($"{nameof(Reservation)}: {nameof(seatIds)} contains an empty identifier.");
        }

        if (reservedSeatIds.Distinct().Count() != reservedSeatIds.Count)
        {
            throw new DomainException($"{nameof(Reservation)}: {nameof(seatIds)} contains duplicate identifiers.");
        }

        if (customerId.IsEmpty)
        {
            throw new DomainException($"{nameof(Reservation)}: {nameof(customerId)} is empty.");
        }

        if (reservationDuration <= TimeSpan.Zero)
        {
            throw new DomainException($"{nameof(Reservation)}: {nameof(reservationDuration)} must be positive.");
        }

        var reservation = new Reservation
        {
            Id = ReservationId.New(),
            CustomerId = customerId,
            CreatedAt = reservedAt,
            ExpiresAt = reservedAt + reservationDuration,
            Status = ReservationStatus.Pending
        };

        reservation._seatIds.AddRange(reservedSeatIds);

        return reservation;
    }

    /// <summary>
    /// Confirms the pending reservation.
    /// </summary>
    /// <param name="confirmedAt">Timestamp at which the reservation is confirmed.</param>
    /// <exception cref="DomainException">
    /// Thrown when the reservation cannot be confirmed from its current state or at the specified time.
    /// </exception>
    public void Confirm(DateTimeOffset confirmedAt)
    {
        if (Status is ReservationStatus.Confirmed)
        {
            return;
        }

        EnsureCanConfirm(confirmedAt);

        ConfirmedAt = confirmedAt;
        Status = ReservationStatus.Confirmed;
    }

    /// <summary>
    /// Marks the pending reservation as expired.
    /// </summary>
    /// <param name="expiredAt">Timestamp at which the expiration is processed.</param>
    /// <exception cref="DomainException">
    /// Thrown when the reservation cannot expire from its current state or before its expiration time.
    /// </exception>
    public void Expire(DateTimeOffset expiredAt)
    {
        if (Status is ReservationStatus.Expired)
        {
            return;
        }

        EnsureCanExpire(expiredAt);

        ExpiredAt = expiredAt;
        Status = ReservationStatus.Expired;
    }

    /// <summary>
    /// Cancels the pending reservation.
    /// </summary>
    /// <param name="cancelledAt">Timestamp at which the reservation is canceled.</param>
    /// <exception cref="DomainException">
    /// Thrown when the reservation cannot be canceled from its current state or at the specified time.
    /// </exception>
    public void Cancel(DateTimeOffset cancelledAt)
    {
        if (Status is ReservationStatus.Cancelled)
        {
            return;
        }

        EnsureCanCancel(cancelledAt);

        CancelledAt = cancelledAt;
        Status = ReservationStatus.Cancelled;
    }

    /// <summary>
    /// Determines whether the reservation is active at the specified time.
    /// </summary>
    /// <param name="currentTime">Timestamp at which to evaluate the reservation.</param>
    /// <returns>
    /// <see langword="true"/> when the reservation is pending and the timestamp is within its active period;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool IsActive(DateTimeOffset currentTime) =>
        Status is ReservationStatus.Pending
        && currentTime >= CreatedAt
        && currentTime < ExpiresAt;

    private void EnsureCanConfirm(DateTimeOffset confirmedAt)
    {
        if (Status is not ReservationStatus.Pending)
        {
            throw new DomainException(
                $"{nameof(Reservation)} cannot be confirmed from {Status} status.");
        }

        if (confirmedAt >= ExpiresAt)
        {
            throw new DomainException(
                $"{nameof(Reservation)} has already expired.");
        }

        if (confirmedAt < CreatedAt)
        {
            throw new DomainException(
                $"{nameof(Reservation)} {nameof(confirmedAt)} with {confirmedAt} date" +
                $" cannot be earlier than {nameof(CreatedAt)} with {CreatedAt} date");
        }

    }

    private void EnsureCanExpire(DateTimeOffset expiredAt)
    {
        if (Status is not ReservationStatus.Pending)
        {
            throw new DomainException(
                $"{nameof(Reservation)} cannot be expired from {Status} status.");
        }

        if (expiredAt < ExpiresAt)
        {
            throw new DomainException(
                $"{nameof(Reservation)} has not expired yet.");
        }
    }

    private void EnsureCanCancel(DateTimeOffset cancelledAt)
    {
        if (Status is not ReservationStatus.Pending)
        {
            throw new DomainException(
                $"{nameof(Reservation)} cannot be cancelled from {Status} status.");
        }

        if (cancelledAt < CreatedAt)
        {
            throw new DomainException(
                $"{nameof(Reservation)} {nameof(cancelledAt)} with {cancelledAt} date" +
                $" cannot be earlier than {nameof(CreatedAt)} with {CreatedAt} date");
        }

        if (cancelledAt >= ExpiresAt)
        {
            throw new DomainException(
                $"{nameof(Reservation)} has already expired.");
        }
    }
}
