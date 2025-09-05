using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace HyperVHandler;

public class CreateVSwitchCommand : ICommand
{
    public CreateVSwitchCommand(CreateVSwitchRequest request)
    {
        Request = request;
    }

    public CreateVSwitchRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        dynamic hv = service.GetService();
        OperationResult result = hv.CreateVSwitch(Request.Name, Request.SwitchType);
        return Task.FromResult(result);
    }
}
