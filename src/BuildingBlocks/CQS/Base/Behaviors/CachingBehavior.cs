using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TicketFlow.BuildingBlocks.CQS.Abstractions.Cache;
using TicketFlow.BuildingBlocks.CQS.Primitives.Interfaces;

namespace TicketFlow.BuildingBlocks.CQS.Behaviors;

/// <summary>
///     Pipeline behavior responsible for caching queries.
/// </summary>
/// <typeparam name="TRequest">Type of the request.</typeparam>
/// <typeparam name="TResponse">Type of the response.</typeparam>
public class CachingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableQuery<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ICacheKeyGenerator<TRequest> _keyGenerator;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IDistributedCache cache,
        ICacheKeyGenerator<TRequest> keyGenerator, 
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _keyGenerator = keyGenerator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var requestName = typeof(TRequest).Name;
        var cacheKey = _keyGenerator.GenerateKey(request);

        // Raw cached value.
        byte[]? cached = null;

        // Fail-open behavior.
        // If the caching service is unavailable, we suppress the errors so that the handler runs, but we log the issue itself.
        // But if cancellation requested - throw, because in that case, the handler should not be executed.
        try
        {
            cached = await _cache.GetAsync(cacheKey, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Response of request {RequestName} cannot get value from cache with exception.",
                requestName);
        }

        // Cache-HIT behavior
        if (cached is not null)
        {
            try
            {
                var value = JsonSerializer.Deserialize<TResponse>(cached);

                if (value is not null)
                {
                    _logger.LogInformation(
                        "Response of request {RequestName} get value from using cache key {CacheKey}",
                        requestName,
                        cacheKey);

                    return value;
                }
            }
            // Fail-open too.
            // Do not interrupt the handler if the cache service is unavailable.
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                _logger.LogWarning(ex,
                    "Response of {RequestName} with using cache key {CacheKey} cannot be deserialized.",
                    requestName,
                    cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Response of {RequestName} with using cache key {CacheKey} cannot be read with exception.",
                    requestName,
                    cacheKey);
            }
        }
        
        // Cache-MISS behavior
        var response = await next(cancellationToken);
        
        try
        {
            var serializedCache = JsonSerializer.SerializeToUtf8Bytes(response);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) // Temporary hard-coded value
            };

            await _cache.SetAsync(cacheKey, serializedCache, options, cancellationToken);

            _logger.LogInformation("Response of {RequestName} cached under key {CacheKey}",
                requestName,
                cacheKey);
        }
        
        // Same fail-open. Throw only if user requests cancellation.
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            _logger.LogWarning(ex,
                "Response of {RequestName} cannot be serialized with key {CacheKey}",
                requestName,
                cacheKey);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Response of {RequestName} with using cache key {CacheKey} cannot be wrote with exception.",
                requestName,
                cacheKey);
        }

        return response;
    }
}