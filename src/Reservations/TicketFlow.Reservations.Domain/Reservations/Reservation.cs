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
    /// <summary>
    /// Identifier of the reserved seat.
    /// </summary>
    public SeatId SeatId { get; private set; }

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

    public ReservationStatus Status { get; private set; }
    
    private Reservation()
    {
    }

    /// <summary>
    /// Creates a new pending reservation for a seat and customer.
    /// </summary>
    /// <param name="seatId">Identifier of the seat to reserve.</param>
    /// <param name="customerId">Identifier of the customer making the reservation.</param>
    /// <param name="reservedAt">Timestamp at which the reservation is created.</param>
    /// <param name="reservationDuration">Duration for which the reservation remains active.</param>
    /// <returns>A new pending reservation.</returns>
    /// <exception cref="DomainException">
    /// Thrown when an identifier is empty or the reservation duration is not positive.
    /// </exception>
    public static Reservation Reserve(
        SeatId seatId,
        CustomerId customerId,
        DateTimeOffset reservedAt,
        TimeSpan reservationDuration)
    {
        if (seatId.IsEmpty)
        {
            throw new DomainException($"{nameof(Reservation)}: {nameof(seatId)} is empty.");
        }

        if (customerId.IsEmpty)
        {
            throw new DomainException($"{nameof(Reservation)}: {nameof(customerId)} is empty.");
        }

        if (reservationDuration <= TimeSpan.Zero)
        {
            throw new DomainException($"{nameof(Reservation)}: {nameof(reservationDuration)} have invalid value.");
        }

        return new Reservation
        {
            Id = ReservationId.New(),
            SeatId = seatId,
            CustomerId = customerId,
            CreatedAt = reservedAt,
            ExpiresAt = reservedAt + reservationDuration,
            Status = ReservationStatus.Pending
        };
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
    /// <param name="cancelledAt">Timestamp at which the reservation is cancelled.</param>
    /// <exception cref="DomainException">
    /// Thrown when the reservation cannot be cancelled from its current state or at the specified time.
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

        if (confirmedAt >= ExpiresAt)
        {
            throw new DomainException(
                $"{nameof(Reservation)} {nameof(confirmedAt)} with {confirmedAt} date" +
                $" cannot be later than {nameof(ExpiresAt)} with {ExpiresAt} date");
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
