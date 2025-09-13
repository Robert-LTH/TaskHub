using System;
using System.Collections.Generic;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace TaskHub.Server;

/// <summary>
/// ILogger wrapper that forwards to an inner logger and mirrors
/// messages into the Hangfire PerformContext console with an optional prefix.
/// </summary>
public sealed class JobConsoleLogger : ILogger
{
    private readonly ILogger _inner;
    private readonly PerformContext? _context;
    private readonly string _prefix;
    private readonly string _jobId;
    private readonly IJobLogStore _store;
    private readonly IEnumerable<ILogPublisher> _publishers;
    private readonly Func<string, string?> _callbackAccessor;

    public JobConsoleLogger(ILogger inner, PerformContext? context, string? prefix, string jobId,
        IJobLogStore store, IEnumerable<ILogPublisher> publishers, Func<string, string?> callbackAccessor)
    {
        _inner = inner;
        _context = context;
        _prefix = string.IsNullOrEmpty(prefix) ? string.Empty : prefix + " ";
        _jobId = jobId;
        _store = store;
        _publishers = publishers;
        _callbackAccessor = callbackAccessor;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);
    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Always forward to the inner logger
        _inner.Log(logLevel, eventId, state, exception, formatter);

        var message = formatter(state, exception);
        var fullMessage = _prefix + message;

        try
        {
            _store.Append(_jobId, fullMessage);
            var callback = _callbackAccessor(_jobId);
            foreach (var pub in _publishers)
            {
                try { pub.PublishLog(_jobId, fullMessage, callback); } catch { }
            }
        }
        catch { }

        // Mirror to Hangfire console when available
        if (_context == null) return;
        try
        {
            var color = logLevel switch
            {
                LogLevel.Trace => ConsoleTextColor.Gray,
                LogLevel.Debug => ConsoleTextColor.Gray,
                LogLevel.Information => ConsoleTextColor.White,
                LogLevel.Warning => ConsoleTextColor.Yellow,
                LogLevel.Error => ConsoleTextColor.Red,
                LogLevel.Critical => ConsoleTextColor.Magenta,
                _ => ConsoleTextColor.White
            };
            _context.SetTextColor(color);
            _context.WriteLine(fullMessage);
            if (exception != null)
            {
                _context.SetTextColor(ConsoleTextColor.DarkRed);
                _context.WriteLine(exception.ToString());
            }
            _context.ResetTextColor();
        }
        catch
        {
            // Never allow logging issues to affect jobs
        }
    }
}

