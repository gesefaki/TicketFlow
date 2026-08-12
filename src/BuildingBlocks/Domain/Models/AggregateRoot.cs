using TicketFlow.BuildingBlocks.Domain.Interfaces;

namespace TicketFlow.BuildingBlocks.Domain.Models;

/// <summary>
/// A root responsible for managing child entities.
/// </summary>
/// <typeparam name="TId">Type of unique identifier of model.</typeparam>
public abstract class AggregateRoot<TId> 
    : Entity<TId>, IHasDomainEvents
    where TId : struct
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Adds a domain event to the domain events collection.
    /// </summary>
    /// <param name="domainEvent">Domain event to add.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="domainEvent"/> is <see langword="null"/>.
    /// </exception>
    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    IReadOnlyList<IDomainEvent> IHasDomainEvents.GetDomainEvents()
        => Array.AsReadOnly(_domainEvents.ToArray());

    /// <inheritdoc />
    void IHasDomainEvents.MarkDomainEventsAsDispatched(IReadOnlyCollection<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (events.Count == 0)
        {
            return;
        }

        var dispatchedEvents = events.ToArray();
        var matchedIndexes = new bool[_domainEvents.Count];

        // First, we'll validate the entire batch without making any changes.
        foreach (var dispatchedEvent in dispatchedEvents)
        {
            var matchedIndex = -1;

            for (var index = 0; index < _domainEvents.Count; index++)
            {
                if (!matchedIndexes[index] && ReferenceEquals(_domainEvents[index], dispatchedEvent))
                {
                    matchedIndex = index;
                    break;
                }
            }

            if (matchedIndex < 0)
            {
                throw new InvalidOperationException(
                    "Cannot acknowledge a domain event that is not pending.");
            }

            matchedIndexes[matchedIndex] = true;
        }

        // Delete from the end so that the indices don't shift.
        for (var index = matchedIndexes.Length - 1; index >= 0; index--)
        {
            if (matchedIndexes[index])
            {
                _domainEvents.RemoveAt(index);
            }
        }
    }
}
