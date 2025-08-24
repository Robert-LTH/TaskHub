using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace CleanTempHandler;

public class CleanTempCommand : ICommand
{
    public CleanTempCommand(CleanTempRequest request)
    {
        Request = request;
    }

    public CleanTempRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        var cleaner = (Action<string>)service.GetService();
        cleaner(Request.Path);
        var element = JsonSerializer.SerializeToElement(Request.Path);
        return Task.FromResult(new OperationResult(element, "success"));
    }
}
