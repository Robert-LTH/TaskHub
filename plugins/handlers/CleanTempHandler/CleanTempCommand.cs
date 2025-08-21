using System;
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

    public Task ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        var cleaner = (Action<string>)service.GetService();
        cleaner(_path);
        return Task.CompletedTask;
    }
}
