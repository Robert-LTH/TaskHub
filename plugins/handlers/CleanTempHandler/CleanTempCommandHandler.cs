using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace CleanTempHandler;

public class CleanTempCommandHandler :
    ICommandHandler<CleanTempCommand>,
    ICommandHandler<DeleteFolderCommand>
{
    public IReadOnlyCollection<string> Commands => new[] { "clean-temp", "delete-folder" };
    public string ServiceName => "filesystem";

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

    public ICommand Create(JsonElement payload) =>
        ((ICommandHandler<CleanTempCommand>)this).Create(payload);
}
