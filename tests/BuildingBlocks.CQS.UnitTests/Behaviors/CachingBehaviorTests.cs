using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using TicketFlow.BuildingBlocks.CQS.Abstractions.Cache;
using TicketFlow.BuildingBlocks.CQS.Behaviors;
using TicketFlow.BuildingBlocks.CQS.Primitives.Interfaces;
using TicketFlow.BuildingBlocks.CQS.UnitTests.Helpers;

namespace TicketFlow.BuildingBlocks.CQS.UnitTests.Behaviors;

public class CachingBehaviorTests
{
    private const string CacheKey = "test-query:5";

    private readonly Mock<IDistributedCache> _cache = new();

    private readonly TestLogger<CachingBehavior<TestQuery, TestResponse>>
        _logger = new();

    private sealed record TestQuery(int Id)
        : ICacheableQuery<TestResponse>;

    private sealed record TestResponse(int Value);

    private sealed class TestCacheKeyGenerator(string cacheKey)
        : ICacheKeyGenerator<TestQuery>
    {
        public string GenerateKey(TestQuery value) => cacheKey;
    }

    private CachingBehavior<TestQuery, TestResponse> CreateBehavior()
    {
        return new CachingBehavior<TestQuery, TestResponse>(
            _cache.Object,
            new TestCacheKeyGenerator(CacheKey),
            _logger);
    }

    [Fact]
    public async Task Handle_WhenCacheContainsValidResponse_ReturnsCachedResponse()
    {
        // Arrange
        var request = new TestQuery(5);
        var cachedResponse = new TestResponse(10);

        _cache
            .Setup(x => x.GetAsync(
                CacheKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                JsonSerializer.SerializeToUtf8Bytes(cachedResponse));

        var nextCalls = 0;
        var behavior = CreateBehavior();

        // Act
        var result = await behavior.Handle(
            request,
            _ =>
            {
                nextCalls++;
                return Task.FromResult(new TestResponse(20));
            },
            CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(cachedResponse);
        nextCalls.Should().Be(0);

        _cache.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ExecutesHandlerAndCachesResponse()
    {
        // Arrange
        var request = new TestQuery(5);
        var handlerResponse = new TestResponse(20);
        var cancellationToken = new CancellationTokenSource().Token;

        byte[]? writtenValue = null;
        DistributedCacheEntryOptions? writtenOptions = null;

        _cache
            .Setup(x => x.GetAsync(CacheKey, cancellationToken))
            .ReturnsAsync((byte[]?)null);

        _cache
            .Setup(x => x.SetAsync(
                CacheKey,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                cancellationToken))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (_, value, options, _) =>
                {
                    writtenValue = value;
                    writtenOptions = options;
                })
            .Returns(Task.CompletedTask);

        var nextCalls = 0;
        var behavior = CreateBehavior();

        // Act
        var result = await behavior.Handle(
            request,
            _ =>
            {
                nextCalls++;
                return Task.FromResult(handlerResponse);
            },
            cancellationToken);

        // Assert
        result.Should().BeSameAs(handlerResponse);
        nextCalls.Should().Be(1);

        writtenValue.Should().NotBeNull();

        JsonSerializer
            .Deserialize<TestResponse>(writtenValue!)
            .Should()
            .BeEquivalentTo(handlerResponse);

        writtenOptions.Should().NotBeNull();
        writtenOptions!.AbsoluteExpirationRelativeToNow
            .Should()
            .Be(TimeSpan.FromMinutes(1));

        _cache.Verify(
            x => x.SetAsync(
                CacheKey,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheReadFails_ExecutesHandler()
    {
        // Arrange
        var request = new TestQuery(5);
        var handlerResponse = new TestResponse(20);

        _cache
            .Setup(x => x.GetAsync(
                CacheKey,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache unavailable"));

        var nextCalls = 0;
        var behavior = CreateBehavior();

        // Act
        var result = await behavior.Handle(
            request,
            _ =>
            {
                nextCalls++;
                return Task.FromResult(handlerResponse);
            },
            CancellationToken.None);

        // Assert
        result.Should().BeSameAs(handlerResponse);
        nextCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenCacheReadIsCanceled_PropagatesCancellation()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var cancellationToken = cancellationSource.Token;
        var expectedException =
            new OperationCanceledException(cancellationToken);

        _cache
            .Setup(x => x.GetAsync(CacheKey, cancellationToken))
            .ThrowsAsync(expectedException);

        var nextCalls = 0;
        var behavior = CreateBehavior();

        // Act
        var act = () => behavior.Handle(
            new TestQuery(5),
            _ =>
            {
                nextCalls++;
                return Task.FromResult(new TestResponse(20));
            },
            cancellationToken);

        // Assert
        var assertion = await act
            .Should()
            .ThrowAsync<OperationCanceledException>();

        assertion.Which.Should().BeSameAs(expectedException);
        nextCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenCachedJsonIsInvalid_ExecutesHandler()
    {
        // Arrange
        var handlerResponse = new TestResponse(20);
        var invalidJson = "{ invalid json"u8.ToArray();

        _cache
            .Setup(x => x.GetAsync(
                CacheKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidJson);

        var nextCalls = 0;
        var behavior = CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new TestQuery(5),
            _ =>
            {
                nextCalls++;
                return Task.FromResult(handlerResponse);
            },
            CancellationToken.None);

        // Assert
        result.Should().BeSameAs(handlerResponse);
        nextCalls.Should().Be(1);

        _cache.Verify(
            x => x.SetAsync(
                CacheKey,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheWriteFails_ReturnsHandlerResponse()
    {
        // Arrange
        var handlerResponse = new TestResponse(20);

        _cache
            .Setup(x => x.GetAsync(
                CacheKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _cache
            .Setup(x => x.SetAsync(
                CacheKey,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache unavailable"));

        var nextCalls = 0;
        var behavior = CreateBehavior();

        // Act
        var result = await behavior.Handle(
            new TestQuery(5),
            _ =>
            {
                nextCalls++;
                return Task.FromResult(handlerResponse);
            },
            CancellationToken.None);

        // Assert
        result.Should().BeSameAs(handlerResponse);
        nextCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenCacheWriteIsCanceled_PropagatesOriginalCancellation()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();

        var cancellationToken = cancellationSource.Token;
        var writeCancellation =
            new OperationCanceledException("SetAsync canceled", cancellationToken);
        var removeCancellation =
            new OperationCanceledException("RemoveAsync canceled", cancellationToken);

        _cache
            .Setup(x => x.GetAsync(CacheKey, cancellationToken))
            .ReturnsAsync((byte[]?)null);

        _cache
            .Setup(x => x.SetAsync(
                CacheKey,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                cancellationToken))
            .ThrowsAsync(writeCancellation);
        
        _cache
            .Setup(x => x.RemoveAsync(CacheKey, cancellationToken))
            .ThrowsAsync(removeCancellation);

        var behavior = CreateBehavior();

        // Act
        var act = () => behavior.Handle(
            new TestQuery(5),
            _ => Task.FromResult(new TestResponse(20)),
            cancellationToken);

        // Assert
        var assertion = await act
            .Should()
            .ThrowAsync<OperationCanceledException>();

        assertion.Which.Should().BeSameAs(writeCancellation);
    }
}