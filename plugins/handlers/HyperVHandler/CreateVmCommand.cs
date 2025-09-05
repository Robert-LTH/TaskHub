using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace HyperVHandler;

public class CreateVmCommand : ICommand
{
    public CreateVmCommand(CreateVmRequest request)
    {
        Request = request;
    }

    public CreateVmRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        dynamic hv = service.GetService();
        OperationResult result = hv.CreateVm(Request.Name, Request.VhdPath, Request.SwitchName, Request.MemoryStartupBytes);
        return Task.FromResult(result);
    }
}
