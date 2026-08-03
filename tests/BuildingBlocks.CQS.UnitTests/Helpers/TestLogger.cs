using Microsoft.Extensions.Logging;

namespace TicketFlow.BuildingBlocks.CQS.UnitTests.Helpers;

public sealed record TestRequest;

public sealed record TestResponse(int Value);

public sealed record LogEntry(
    LogLevel Level,
    string Message,
    Exception? Exception);

public sealed class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull 
        => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(
            logLevel,
            formatter(state, exception),
            exception)
        );
    }
}