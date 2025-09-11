using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace CleanTempHandler;

public class CleanTempCommand : ICommand
{
    public CleanTempCommand(CleanTempRequest request)
    {
        Request = request;
    }

    public CleanTempRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        dynamic fs = service.GetService();
        fs.Delete(Request.Path);
        var element = JsonSerializer.SerializeToElement(Request.Path);
        return Task.FromResult(new OperationResult(element, "success"));
    }
}
