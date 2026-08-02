namespace TicketFlow.BuildingBlocks.Domain.Interfaces;

/// <summary>
/// Defines a contract for entities that support soft deletion.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Deletion timestamp, or <see langword="null"/> when the entity is not deleted.
    /// </summary>
    DateTimeOffset? DeletedAt { get; }

    /// <summary>
    /// Marks the entity as deleted.
    /// </summary>
    /// <param name="deletedAt">Timestamp at which the entity was deleted.</param>
    void Delete(DateTimeOffset deletedAt);

    /// <summary>
    /// Restores a previously deleted entity.
    /// </summary>
    void Restore();
}
