using System;
using System.Collections.Generic;
using System.Text.Json;
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

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }

    CreateVmCommand ICommandHandler<CreateVmCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<CreateVmRequest>(payload.GetRawText())
                      ?? new CreateVmRequest();
        return new CreateVmCommand(request);
    }

    CreateVSwitchCommand ICommandHandler<CreateVSwitchCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<CreateVSwitchRequest>(payload.GetRawText())
                      ?? new CreateVSwitchRequest();
        return new CreateVSwitchCommand(request);
    }

    CreateVhdxCommand ICommandHandler<CreateVhdxCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<CreateVhdxRequest>(payload.GetRawText())
                      ?? new CreateVhdxRequest();
        return new CreateVhdxCommand(request);
    }

    public override ICommand Create(JsonElement payload)
    {
        if (payload.ValueKind == JsonValueKind.Object)
        {
            if (payload.TryGetProperty("SizeBytes", out _))
            {
                return ((ICommandHandler<CreateVhdxCommand>)this).Create(payload);
            }
            if (payload.TryGetProperty("SwitchType", out _))
            {
                return ((ICommandHandler<CreateVSwitchCommand>)this).Create(payload);
            }
        }
        return ((ICommandHandler<CreateVmCommand>)this).Create(payload);
    }
}



