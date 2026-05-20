using System;
using System.Security.Principal;
using Microsoft.Extensions.Configuration;
using TaskHub.Abstractions;

namespace TaskHub.Server;

public static class CommandExecutionContextResolver
{
    public static CommandExecutionContext Resolve(IConfiguration? configuration)
    {
        var configured = configuration?["JobHandling:ExecutionContext"];
        if (Enum.TryParse<CommandExecutionContext>(configured, ignoreCase: true, out var context)
            && context != CommandExecutionContext.RegularUserOrSystem)
        {
            return context;
        }

        return IsRunningAsLocalSystem()
            ? CommandExecutionContext.System
            : CommandExecutionContext.RegularUser;
    }

    public static bool CanRun(CommandExecutionContext handlerContext, CommandExecutionContext runnerContext) =>
        handlerContext == CommandExecutionContext.RegularUserOrSystem || handlerContext == runnerContext;

    private static bool IsRunningAsLocalSystem()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            return WindowsIdentity.GetCurrent().User?.IsWellKnown(WellKnownSidType.LocalSystemSid) == true;
        }
        catch
        {
            return false;
        }
    }
}
