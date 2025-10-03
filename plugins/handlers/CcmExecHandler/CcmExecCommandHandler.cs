using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using TaskHub.Abstractions;

namespace CcmExecHandler;

public class CcmExecCommandHandler : CommandHandlerBase, ICommandHandler<TriggerScheduleCommand>, IPluginPrerequisites
{
    public override IReadOnlyCollection<string> Commands => new[] { "ccmexwc" };
    public override string ServiceName => "configurationmanager";

    TriggerScheduleCommand ICommandHandler<TriggerScheduleCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<TriggerScheduleRequest>(payload.GetRawText())
                      ?? new TriggerScheduleRequest();
        return new TriggerScheduleCommand(request);
    }

    public override ICommand Create(JsonElement payload) =>
        ((ICommandHandler<TriggerScheduleCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }

    public bool ShouldLoad(IServiceProvider services, out string? reason)
    {
        reason = null;
        if (!OperatingSystem.IsWindows())
        {
            reason = "Non-Windows OS";
            return false;
        }

        // Simple heuristic: CCM client folder exists
        var ccmFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "CCM");
        if (!Directory.Exists(ccmFolder))
        {
            reason = "Configuration Manager client not installed (CCM folder missing)";
            return false;
        }

        return true;
    }
}



