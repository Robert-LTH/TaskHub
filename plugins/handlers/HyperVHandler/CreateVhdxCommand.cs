using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace HyperVHandler;

public class CreateVhdxCommand : ICommand
{
    public CreateVhdxCommand(CreateVhdxRequest request)
    {
        Request = request;
    }

    public CreateVhdxRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        dynamic hv = service.GetService();
        OperationResult result = hv.CreateVhdx(Request.Path, Request.SizeBytes, Request.Dynamic);
        return Task.FromResult(result);
    }
}
