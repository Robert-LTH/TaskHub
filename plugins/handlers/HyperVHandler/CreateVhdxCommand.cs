using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace HyperVHandler;

public class CreateVhdxCommand : ICommand
{
    private readonly ILogger _logger;

    public CreateVhdxCommand(CreateVhdxRequest request, ILogger logger)
    {
        Request = request;
        _logger = logger;
    }

    public CreateVhdxRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        dynamic hv = service.GetService();
        OperationResult result = hv.CreateVhdx(Request.Path, Request.SizeBytes, Request.Dynamic);
        return Task.FromResult(result);
    }
}
