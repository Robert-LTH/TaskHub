using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;

    QueryCommand ICommandHandler<QueryCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<QueryRequest>(payload.GetRawText()) ?? new QueryRequest();
        return new QueryCommand(request, _useAdminService, logger);
    }

    InvokeMethodCommand ICommandHandler<InvokeMethodCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<InvokeMethodRequest>(payload.GetRawText()) ?? new InvokeMethodRequest();
        return new InvokeMethodCommand(request, _useAdminService, logger);
    }

    GetErrorCodeCommand ICommandHandler<GetErrorCodeCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<GetErrorCodeRequest>(payload.GetRawText()) ?? new GetErrorCodeRequest();
        return new GetErrorCodeCommand(request, _useAdminService, logger);
    }

    AddDeviceToCollectionCommand ICommandHandler<AddDeviceToCollectionCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<AddDeviceToCollectionRequest>(payload.GetRawText()) ?? new AddDeviceToCollectionRequest();
        return new AddDeviceToCollectionCommand(request, _useAdminService, logger);
    }

    AddUserToCollectionCommand ICommandHandler<AddUserToCollectionCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<AddUserToCollectionRequest>(payload.GetRawText()) ?? new AddUserToCollectionRequest();
        return new AddUserToCollectionCommand(request, logger, _useAdminService);
    }

    public override ICommand Create(JsonElement payload, ILogger logger)
    {
        if (payload.ValueKind == JsonValueKind.Object)
        {
            if (payload.TryGetProperty("UserIds", out _))
            {
                return ((ICommandHandler<AddUserToCollectionCommand>)this).Create(payload, logger);
            }

            if (payload.TryGetProperty("DeviceIds", out _))
            {
                return ((ICommandHandler<AddDeviceToCollectionCommand>)this).Create(payload, logger);
            }

            if (payload.TryGetProperty("Method", out _))
            {
                return ((ICommandHandler<InvokeMethodCommand>)this).Create(payload, logger);
            }

            if (payload.TryGetProperty("PnpDeviceId", out _))
            {
                return ((ICommandHandler<GetErrorCodeCommand>)this).Create(payload, logger);
            }
        }

        return ((ICommandHandler<QueryCommand>)this).Create(payload, logger);
    }

    public override ICommand Create(string command, JsonElement payload, ILogger logger)
    {
        return command switch
        {
            "cm-query" => ((ICommandHandler<QueryCommand>)this).Create(payload, logger),
            "cm-invoke" => ((ICommandHandler<InvokeMethodCommand>)this).Create(payload, logger),
            "cm-errorcode" => ((ICommandHandler<GetErrorCodeCommand>)this).Create(payload, logger),
            "cm-adddevice" => ((ICommandHandler<AddDeviceToCollectionCommand>)this).Create(payload, logger),
            "cm-adduser" => ((ICommandHandler<AddUserToCollectionCommand>)this).Create(payload, logger),
            _ => throw new InvalidOperationException($"Unsupported command '{command}'")
        };
    }

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}

