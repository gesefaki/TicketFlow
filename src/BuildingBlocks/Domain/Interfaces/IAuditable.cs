namespace TicketFlow.BuildingBlocks.Domain.Interfaces;

/// <summary>
/// Defines a contract for performing an entity audit.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Creation timestamp.
    /// </summary>
    DateTimeOffset CreatedAt { get; }
    
    /// <summary>
    /// Updating timestamp.
    /// </summary>
    DateTimeOffset? UpdatedAt { get; }
}