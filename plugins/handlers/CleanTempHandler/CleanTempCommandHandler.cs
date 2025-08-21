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
        var path = payload.TryGetProperty("path", out var element) ? element.GetString() : @"C:\\temp22";
        return new CleanTempCommand(path ?? @"C:\\temp22");
    }

    DeleteFolderCommand ICommandHandler<DeleteFolderCommand>.Create(JsonElement payload)
    {
        var path = payload.TryGetProperty("path", out var element) ? element.GetString() : @"C:\\temp22";
        return new DeleteFolderCommand(path ?? @"C:\\temp22");
    }

    public ICommand Create(JsonElement payload) =>
        ((ICommandHandler<CleanTempCommand>)this).Create(payload);
}
