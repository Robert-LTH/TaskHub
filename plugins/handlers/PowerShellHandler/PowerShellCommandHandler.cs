using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;
using PowerShellServicePlugin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;

namespace PowerShellHandler;

public class PowerShellCommandHandler : CommandHandlerBase, ICommandHandler<PowerShellCommand>
{
    private readonly ILogger _logger;
    public PowerShellCommandHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PowerShellCommandHandler>();
    }

    // Back-compat for tests and callers not using DI
    public PowerShellCommandHandler() : this(NullLoggerFactory.Instance) { }

    public override IReadOnlyCollection<string> Commands => new[] { "powershell-script" };
    public override string ServiceName => "powershell";

    PowerShellCommand ICommandHandler<PowerShellCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<PowerShellScriptRequest>(payload.GetRawText())
                      ?? new PowerShellScriptRequest();
        //var logger = _loggerFactory.CreateLogger(nameof(PowerShellCommand));
        return new PowerShellCommand(request, _logger);
    }

    public override ICommand Create(JsonElement payload) =>
        ((ICommandHandler<PowerShellCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services) { }
}
