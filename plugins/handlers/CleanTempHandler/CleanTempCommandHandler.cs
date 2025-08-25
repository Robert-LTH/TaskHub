using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Abstractions;
using TaskHub.Server;

namespace CleanTempHandler;

public class CleanTempCommandHandler :
    CommandHandlerBase,
    ICommandHandler<CleanTempCommand>,
    ICommandHandler<DeleteFolderCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "clean-temp", "delete-folder" };
    public override string ServiceName => "filesystem";

    CleanTempCommand ICommandHandler<CleanTempCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<CleanTempRequest>(payload.GetRawText())
                      ?? new CleanTempRequest();
        return new CleanTempCommand(request);
    }

    DeleteFolderCommand ICommandHandler<DeleteFolderCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<DeleteFolderRequest>(payload.GetRawText())
                      ?? new DeleteFolderRequest();
        return new DeleteFolderCommand(request);
    }

    public override ICommand Create(JsonElement payload) =>
        ((ICommandHandler<CleanTempCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        var recurringJobs = services.GetRequiredService<IRecurringJobManager>();
        var payload = JsonSerializer.Deserialize<JsonElement>("{}");
        recurringJobs.AddOrUpdate<CommandExecutor>(
            "clean-temp",
            exec => exec.Execute("clean-temp", payload, CancellationToken.None),
            Cron.HourInterval(7));
    }
}
