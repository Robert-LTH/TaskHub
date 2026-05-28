using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace HyperVHandler;

public class HyperVCommandHandler : CommandHandlerBase,
    ICommandHandler<CreateVmCommand>,
    ICommandHandler<CreateVSwitchCommand>,
    ICommandHandler<CreateVhdxCommand>
{
    public override IReadOnlyCollection<string> Commands =>
        new[] { "hyperv-create-vm", "hyperv-create-switch", "hyperv-create-vhdx" };

    public override string ServiceName => "hyperv";

    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.System;

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }

    CreateVmCommand ICommandHandler<CreateVmCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<CreateVmRequest>(payload.GetRawText())
                      ?? new CreateVmRequest();
        return new CreateVmCommand(request, logger);
    }

    CreateVSwitchCommand ICommandHandler<CreateVSwitchCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<CreateVSwitchRequest>(payload.GetRawText())
                      ?? new CreateVSwitchRequest();
        return new CreateVSwitchCommand(request, logger);
    }

    CreateVhdxCommand ICommandHandler<CreateVhdxCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<CreateVhdxRequest>(payload.GetRawText())
                      ?? new CreateVhdxRequest();
        return new CreateVhdxCommand(request, logger);
    }

    public override ICommand Create(JsonElement payload, ILogger logger)
    {
        if (payload.ValueKind == JsonValueKind.Object)
        {
            if (payload.TryGetProperty("SizeBytes", out _))
            {
                return ((ICommandHandler<CreateVhdxCommand>)this).Create(payload, logger);
            }
            if (payload.TryGetProperty("SwitchType", out _))
            {
                return ((ICommandHandler<CreateVSwitchCommand>)this).Create(payload, logger);
            }
        }
        return ((ICommandHandler<CreateVmCommand>)this).Create(payload, logger);
    }

    public override ICommand Create(string command, JsonElement payload, ILogger logger)
    {
        return command switch
        {
            "hyperv-create-vm" => ((ICommandHandler<CreateVmCommand>)this).Create(payload, logger),
            "hyperv-create-switch" => ((ICommandHandler<CreateVSwitchCommand>)this).Create(payload, logger),
            "hyperv-create-vhdx" => ((ICommandHandler<CreateVhdxCommand>)this).Create(payload, logger),
            _ => throw new InvalidOperationException($"Unsupported command '{command}'")
        };
    }
}

