using System;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace TaskHub.Server;

public class PerformContextLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new PerformContextLogger(categoryName);
    public void Dispose() { }

    private sealed class PerformContextLogger : ILogger
    {
        private readonly string _category;
        public PerformContextLogger(string category) => _category = category;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var ctx = JobLogContext.Current;
            if (ctx == null) return;
            try
            {
                var msg = formatter(state, exception);
                var line = string.IsNullOrEmpty(_category) ? msg : $"[{_category}] {msg}";
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
                ctx.SetTextColor(color);
                ctx.WriteLine(line);
                if (exception != null)
                {
                    ctx.SetTextColor(ConsoleTextColor.DarkRed);
                    ctx.WriteLine(exception.ToString());
                }
                ctx.ResetTextColor();
            }
            catch
            {
                // Swallow logging errors to avoid impacting job execution
            }
        }
    }
}

