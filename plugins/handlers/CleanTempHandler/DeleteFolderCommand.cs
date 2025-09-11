using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace CleanTempHandler;

public class DeleteFolderCommand : ICommand
{
    public DeleteFolderCommand(DeleteFolderRequest request)
    {
        Request = request;
    }

    public DeleteFolderRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        if (Directory.Exists(Request.Path))
        {
            Directory.Delete(Request.Path, true);
        }

        var element = JsonSerializer.SerializeToElement(Request.Path);
        return Task.FromResult(new OperationResult(element, "success"));
    }
}

