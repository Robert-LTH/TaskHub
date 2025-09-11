using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace HyperVHandler;

public class CreateVSwitchCommand : ICommand
{
    public CreateVSwitchCommand(CreateVSwitchRequest request)
    {
        Request = request;
    }

    public CreateVSwitchRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        dynamic hv = service.GetService();
        OperationResult result = hv.CreateVSwitch(Request.Name, Request.SwitchType);
        return Task.FromResult(result);
    }
}
