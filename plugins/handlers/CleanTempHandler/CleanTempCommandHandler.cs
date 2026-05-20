using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

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

    public override ICommand Create(string command, JsonElement payload)
    {
        return command switch
        {
            "clean-temp" => ((ICommandHandler<CleanTempCommand>)this).Create(payload),
            "delete-folder" => ((ICommandHandler<DeleteFolderCommand>)this).Create(payload),
            _ => throw new InvalidOperationException($"Unsupported command '{command}'")
        };
    }

    public override void OnLoaded(IServiceProvider services) => base.OnLoaded(services);
}
