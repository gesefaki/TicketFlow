using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TicketFlow.BuildingBlocks.CQS.Behaviors;
using TicketFlow.BuildingBlocks.CQS.UnitTests.Helpers;

namespace TicketFlow.BuildingBlocks.CQS.UnitTests.Behaviors;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_WhenHandlerSucceeds_LogsStartAndCompletionAndReturnsResponse()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = "GET",
                Path = "/tickets/5"
            }
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var logger
            = new TestLogger<LoggingBehavior<TestRequest, TestResponse>>();

        var behavior
            = new LoggingBehavior<TestRequest, TestResponse>(logger, accessor);

        var expectedResponse = new TestResponse(5);
        var handlerCalls = 0;
        var observedToken = CancellationToken.None;

        using var cancellationSource = new CancellationTokenSource();
        
        // Act
        var result = await behavior.Handle(
            new TestRequest(),
            cancellationToken =>
            {
                handlerCalls++;
                observedToken = cancellationToken;

                return Task.FromResult(expectedResponse);
            },
            cancellationSource.Token
        );
        
        // Assert
        result.Should().Be(expectedResponse);
        handlerCalls.Should().Be(1);
        observedToken.Should().Be(cancellationSource.Token);

        logger.Entries.Should().HaveCount(2);
        logger.Entries.Should()
            .OnlyContain(entry => entry.Level == LogLevel.Information);
        
        logger.Entries[0].Message.Should().Contain("started");
        logger.Entries[0].Message.Should().Contain(nameof(TestRequest));
        logger.Entries[0].Message.Should().Contain("/tickets/5");
        logger.Entries[0].Message.Should().Contain("GET");

        logger.Entries[1].Message.Should().Contain("completed");
        logger.Entries[1].Message.Should().Contain(nameof(TestRequest));
        logger.Entries[1].Message.Should().Contain("/tickets/5");
        logger.Entries[1].Message.Should().Contain("GET");
        logger.Entries.Should().OnlyContain(entry => entry.Exception == null);
    }

    [Fact]
    public async Task Handle_WhenHandlerFails_LogsErrorAndPropagatesOriginalException()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = "POST",
                Path = "/tickets"
            }
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var logger =
            new TestLogger<LoggingBehavior<TestRequest, TestResponse>>();

        var behavior =
            new LoggingBehavior<TestRequest, TestResponse>(logger, accessor);

        var expectedException = new InvalidOperationException("Handler failed");

        // Act
        Func<Task> action = () => behavior.Handle(
            new TestRequest(),
            _ => Task.FromException<TestResponse>(expectedException),
            CancellationToken.None);

        // Assert
        var assertion = await action.Should()
            .ThrowAsync<InvalidOperationException>();

        assertion.Which.Should().BeSameAs(expectedException);

        logger.Entries.Should().HaveCount(2);
        logger.Entries[0].Level.Should().Be(LogLevel.Information);
        logger.Entries[0].Message.Should().Contain("started");
        logger.Entries[0].Exception.Should().BeNull();

        logger.Entries[1].Level.Should().Be(LogLevel.Error);
        logger.Entries[1].Message.Should().Contain("failed");
        logger.Entries[1].Message.Should().Contain(nameof(TestRequest));
        logger.Entries[1].Message.Should().Contain("/tickets");
        logger.Entries[1].Message.Should().Contain("POST");
        logger.Entries[1].Exception.Should().BeSameAs(expectedException);
        logger.Entries.Should().NotContain(entry => entry.Message.Contains("completed"));
    }
}
