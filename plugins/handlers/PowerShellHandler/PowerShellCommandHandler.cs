using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;
using PowerShellServicePlugin;

namespace PowerShellHandler;

public class PowerShellCommandHandler : CommandHandlerBase, ICommandHandler<PowerShellCommand>
{
    private readonly IServicePlugin _service;

    public PowerShellCommandHandler(PowerShellServicePlugin.PowerShellServicePlugin service)
    {
        _service = service;
    }

    public override IReadOnlyCollection<string> Commands => new[] { "powershell-script" };
    public override string ServiceName => "powershell";

    PowerShellCommand ICommandHandler<PowerShellCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<PowerShellScriptRequest>(payload.GetRawText())
                      ?? new PowerShellScriptRequest();
        return new PowerShellCommand(_service, request);
    }

    public override ICommand Create(JsonElement payload) =>
        ((ICommandHandler<PowerShellCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services) { }
}
