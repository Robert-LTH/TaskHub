using System.Diagnostics;
using Hangfire.Console;

namespace TaskHub.Server;

public class PerformContextTraceListener : TraceListener
{
    public override void Write(string? message) => WriteLine(message);

    public override void WriteLine(string? message)
    {
        var ctx = JobLogContext.Current;
        if (ctx == null || string.IsNullOrEmpty(message)) return;
        ctx.SetTextColor(ConsoleTextColor.Gray);
        ctx.WriteLine(message);
        ctx.ResetTextColor();
    }
}
