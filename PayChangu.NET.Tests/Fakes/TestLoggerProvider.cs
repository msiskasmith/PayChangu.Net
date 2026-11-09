using Microsoft.Extensions.Logging;

namespace PayChangu.NET.Tests.Fakes;

public sealed class TestLoggerProvider : ILoggerProvider
{
    public List<LogEntry> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(Entries);
    }

    public void Dispose()
    {
    }

    private sealed class TestLogger : ILogger
    {
        private readonly IList<LogEntry> _entries;

        public TestLogger(IList<LogEntry> entries)
        {
            _entries = entries;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _entries.Add(new LogEntry(
                logLevel,
                formatter(state, exception),
                exception));
        }
    }
}

public sealed record LogEntry(
    LogLevel LogLevel,
    string Message,
    Exception? Exception);
