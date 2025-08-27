using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TaskHub.Abstractions;

namespace ConfigurationManagerHandler;

public class ConfigurationManagerCommandHandler : CommandHandlerBase,
    ICommandHandler<QueryCommand>,
    ICommandHandler<InvokeMethodCommand>,
    ICommandHandler<GetErrorCodeCommand>,
    ICommandHandler<AddDeviceToCollectionCommand>,
    ICommandHandler<AddUserToCollectionCommand>
{
    private readonly bool _useAdminService;

    public ConfigurationManagerCommandHandler(IConfiguration? config = null)
    {
        _useAdminService = config?.GetValue<bool>("PluginSettings:ConfigurationManager:UseAdminService") ?? false;
    }

    public override IReadOnlyCollection<string> Commands =>
        new[] { "cm-query", "cm-invoke", "cm-errorcode", "cm-adddevice", "cm-adduser" };

    public override string ServiceName => _useAdminService ? "configurationmanageradmin" : "configurationmanager";

    QueryCommand ICommandHandler<QueryCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<QueryRequest>(payload.GetRawText()) ?? new QueryRequest();
        return new QueryCommand(request, _useAdminService);
    }

    InvokeMethodCommand ICommandHandler<InvokeMethodCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<InvokeMethodRequest>(payload.GetRawText()) ?? new InvokeMethodRequest();
        return new InvokeMethodCommand(request, _useAdminService);
    }

    GetErrorCodeCommand ICommandHandler<GetErrorCodeCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<GetErrorCodeRequest>(payload.GetRawText()) ?? new GetErrorCodeRequest();
        return new GetErrorCodeCommand(request, _useAdminService);
    }

    AddDeviceToCollectionCommand ICommandHandler<AddDeviceToCollectionCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<AddDeviceToCollectionRequest>(payload.GetRawText()) ?? new AddDeviceToCollectionRequest();
        return new AddDeviceToCollectionCommand(request, _useAdminService);
    }

    AddUserToCollectionCommand ICommandHandler<AddUserToCollectionCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<AddUserToCollectionRequest>(payload.GetRawText()) ?? new AddUserToCollectionRequest();
        return new AddUserToCollectionCommand(request, _useAdminService);
    }

    public override ICommand Create(JsonElement payload)
    {
        if (payload.ValueKind == JsonValueKind.Object)
        {
            if (payload.TryGetProperty("UserIds", out _))
            {
                return ((ICommandHandler<AddUserToCollectionCommand>)this).Create(payload);
            }

            if (payload.TryGetProperty("DeviceIds", out _))
            {
                return ((ICommandHandler<AddDeviceToCollectionCommand>)this).Create(payload);
            }

            if (payload.TryGetProperty("Method", out _))
            {
                return ((ICommandHandler<InvokeMethodCommand>)this).Create(payload);
            }

            if (payload.TryGetProperty("PnpDeviceId", out _))
            {
                return ((ICommandHandler<GetErrorCodeCommand>)this).Create(payload);
            }
        }

        return ((ICommandHandler<QueryCommand>)this).Create(payload);
    }

    public override void OnLoaded(IServiceProvider services) { }
}
