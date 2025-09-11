using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;
using PowerShellServicePlugin;
using Microsoft.Extensions.Logging;

namespace PowerShellHandler;

public class PowerShellCommandHandler : CommandHandlerBase, ICommandHandler<PowerShellCommand>
{
    private ILoggerFactory? _loggerFactory;
    public PowerShellCommandHandler() { }

    public override IReadOnlyCollection<string> Commands => new[] { "powershell-script" };
    public override string ServiceName => "powershell";

    PowerShellCommand ICommandHandler<PowerShellCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<PowerShellScriptRequest>(payload.GetRawText())
                      ?? new PowerShellScriptRequest();
        var logger = _loggerFactory?.CreateLogger(nameof(PowerShellCommand)) ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        logger.LogDebug("PSCHandler test");
        return new PowerShellCommand(request, logger);
    }

    public override ICommand Create(JsonElement payload) =>
        ((ICommandHandler<PowerShellCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        _loggerFactory = (ILoggerFactory?)services.GetService(typeof(ILoggerFactory));
    }
}
