using System.Threading;
using Hangfire.Server;

namespace TaskHub.Server;

internal static class JobLogContext
{
    private static readonly AsyncLocal<PerformContext?> _current = new();
    public static PerformContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}

