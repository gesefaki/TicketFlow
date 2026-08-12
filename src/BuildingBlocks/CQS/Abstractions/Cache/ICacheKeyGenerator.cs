namespace TicketFlow.BuildingBlocks.CQS.Abstractions.Cache;

/// <summary>
/// A temporary placeholder interface that hasn't been fully implemented yet.
/// </summary>
/// <typeparam name="TRequest">Type of request for which a cache key is generated.</typeparam>
public interface ICacheKeyGenerator<in TRequest>
{
    /// <summary>
    /// Creates a unique entity key for storage in the cache.
    /// </summary>
    /// <param name="value">The value used to generate the key.</param>
    /// <returns>A unique key for caching.</returns>
    public string GenerateKey(TRequest value);
}
