using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace CleanTempHandler;

public class CleanTempCommandHandler :
    CommandHandlerBase,
    ICommandHandler<CleanTempCommand>,
    ICommandHandler<DeleteFolderCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "clean-temp", "delete-folder" };
    public override string ServiceName => "filesystem";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;

    CleanTempCommand ICommandHandler<CleanTempCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<CleanTempRequest>(payload.GetRawText())
                      ?? new CleanTempRequest();
        return new CleanTempCommand(request, logger);
    }

    DeleteFolderCommand ICommandHandler<DeleteFolderCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<DeleteFolderRequest>(payload.GetRawText())
                      ?? new DeleteFolderRequest();
        return new DeleteFolderCommand(request, logger);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) =>
        ((ICommandHandler<CleanTempCommand>)this).Create(payload, logger);

    public override ICommand Create(string command, JsonElement payload, ILogger logger)
    {
        return command switch
        {
            "clean-temp" => ((ICommandHandler<CleanTempCommand>)this).Create(payload, logger),
            "delete-folder" => ((ICommandHandler<DeleteFolderCommand>)this).Create(payload, logger),
            _ => throw new InvalidOperationException($"Unsupported command '{command}'")
        };
    }

    public override void OnLoaded(IServiceProvider services) => base.OnLoaded(services);
}
