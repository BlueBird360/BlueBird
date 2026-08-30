using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace BlueBird.Http.Logging.Tests;

/// <summary>
/// A simple <see cref="ILogger"/> implementation that captures all log entries
/// for inspection in tests.
/// </summary>
public sealed class TestLogger : ILogger
{
    public List<(
        LogLevel Level,
        EventId EventId,
        IReadOnlyDictionary<string, object?> Properties,
        string Message,
        Exception? Exception)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var properties = new Dictionary<string, object?>();
        if (state is IEnumerable<KeyValuePair<string, object?>> values)
        {
            foreach (var value in values)
            {
                properties[value.Key] = value.Value;
            }
        }

        this.Entries.Add((logLevel, eventId, properties, formatter(state, exception), exception));
    }
}

internal sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly TestLogger _logger;

    public TestLoggerProvider(TestLogger logger)
    {
        this._logger = logger;
    }

    public List<string> CategoryNames { get; } = new();

    public ILogger CreateLogger(string categoryName)
    {
        this.CategoryNames.Add(categoryName);
        return this._logger;
    }

    public void Dispose()
    {
    }
}
