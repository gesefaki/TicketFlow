namespace TicketFlow.BuildingBlocks.Domain.Interfaces;

/// <summary>
/// Defines the contract for implementing domain event storage.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets an immutable snapshot of the pending domain events.
    /// </summary>
    /// <returns>Immutable collection of <see cref="IDomainEvent"/>.</returns>
    IReadOnlyList<IDomainEvent> GetDomainEvents();
    
    /// <summary>
    /// Removes the specified successfully dispatched events from the pending events collection.
    /// </summary>
    /// <remarks>
    /// Events are matched by reference identity. The operation is atomic: when any supplied event
    /// is not pending, no events are removed.
    /// </remarks>
    /// <param name="events">Events that were successfully dispatched.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="events"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when at least one supplied event is not pending.
    /// </exception>
    void MarkDomainEventsAsDispatched(IReadOnlyCollection<IDomainEvent> events);
}
