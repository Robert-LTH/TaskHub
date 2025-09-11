using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;
using PowerShellServicePlugin;

namespace PowerShellHandler;

public class PowerShellCommandHandler : CommandHandlerBase, ICommandHandler<PowerShellCommand>
{
    public PowerShellCommandHandler() { }

    public override IReadOnlyCollection<string> Commands => new[] { "powershell-script" };
    public override string ServiceName => "powershell";

    PowerShellCommand ICommandHandler<PowerShellCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<PowerShellScriptRequest>(payload.GetRawText())
                      ?? new PowerShellScriptRequest();
        return new PowerShellCommand(request);
    }

    public override ICommand Create(JsonElement payload) =>
        ((ICommandHandler<PowerShellCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services) { }
}
