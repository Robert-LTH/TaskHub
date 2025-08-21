using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace CleanTempHandler;

public class CleanTempCommand : ICommand
{
    private readonly string _path;

    public CleanTempCommand(string path)
    {
        _path = path;
    }

    public Task<JsonElement> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        var cleaner = (Action<string>)service.GetService();
        cleaner(_path);
        return Task.FromResult(JsonSerializer.SerializeToElement(_path));
    }
}
