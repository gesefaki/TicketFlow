using TicketFlow.BuildingBlocks.Domain.Interfaces;

namespace TicketFlow.BuildingBlocks.Domain.Models;

/// <summary>
/// A root responsible for managing child entities.
/// </summary>
/// <typeparam name="TId">Type of unique identifier of model.</typeparam>
public abstract class AggregateRoot<TId> 
    : BaseEntity<TId>, IHasDomainEvents
    where TId : struct
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    /// <summary>
    /// Adds a domain event to the domain events collection.
    /// </summary>
    /// <param name="domainEvent">Domain event to add.</param>
    protected void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}