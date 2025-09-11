using System;
using System.Collections.Generic;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

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

    public JobConsoleLogger(ILogger inner, PerformContext? context, string? prefix = null)
    {
        _inner = inner;
        _context = context;
        _prefix = string.IsNullOrEmpty(prefix) ? string.Empty : prefix + " ";
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);
    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Always forward to the inner logger
        _inner.Log(logLevel, eventId, state, exception, formatter);

        // Mirror to Hangfire console when available
        if (_context == null) return;
        try
        {
            var msg = formatter(state, exception);
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
            _context.WriteLine(_prefix + msg);
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

