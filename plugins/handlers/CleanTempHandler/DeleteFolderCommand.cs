using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace CleanTempHandler;

public class DeleteFolderCommand : ICommand
{
    private readonly string _path;

    public DeleteFolderCommand(string path)
    {
        _path = path;
    }

    public Task<JsonElement> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        if (Directory.Exists(_path))
        {
            Directory.Delete(_path, true);
        }

        return Task.FromResult(JsonSerializer.SerializeToElement(_path));
    }
}

