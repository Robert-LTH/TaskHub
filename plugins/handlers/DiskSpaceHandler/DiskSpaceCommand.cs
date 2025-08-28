using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace DiskSpaceHandler;

public class DiskSpaceCommand : ICommand
{
    private readonly IReportingContainer? _container;

    public DiskSpaceCommand(DiskSpaceRequest request, IReportingContainer? container)
    {
        Request = request;
        _container = container;
    }

    public DiskSpaceRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        dynamic fs = service.GetService();
        OperationResult result = fs.GetFreeSpace(Request.Path);
        if (result.Payload.HasValue)
        {
            _container?.AddReport("disk-free", result.Payload.Value);
        }
        return Task.FromResult(result);
    }
}

