using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace HyperVHandler;

public class CreateVSwitchCommand : ICommand
{
    private readonly ILogger _logger;

    public CreateVSwitchCommand(CreateVSwitchRequest request, ILogger logger)
    {
        Request = request;
        _logger = logger;
    }

    public CreateVSwitchRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        dynamic hv = service.GetService();
        OperationResult result = hv.CreateVSwitch(Request.Name, Request.SwitchType);
        return Task.FromResult(result);
    }
}
