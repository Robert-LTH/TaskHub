using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TaskHub.Abstractions;

namespace ConfigurationManagerHandler;

public class ConfigurationManagerCommandHandler : CommandHandlerBase, ICommandHandler<QueryCommand>
{
    private readonly bool _useAdminService;

    public ConfigurationManagerCommandHandler(IConfiguration? config = null)
    {
        _useAdminService = config?.GetValue<bool>("PluginSettings:ConfigurationManager:UseAdminService") ?? false;
    }

    public override IReadOnlyCollection<string> Commands => new[] { "cm-query" };

    public override string ServiceName => _useAdminService ? "configurationmanageradmin" : "configurationmanager";

    public QueryCommand Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<QueryRequest>(payload.GetRawText()) ?? new QueryRequest();
        return new QueryCommand(request, _useAdminService);
    }

    public override ICommand Create(JsonElement payload) => Create(payload);

    public override void OnLoaded(IServiceProvider services) { }
}
