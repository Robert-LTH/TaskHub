using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace DiskSpaceHandler;

public class DiskSpaceCommand : ICommand
{
    private readonly IReportingContainer? _container;
    private readonly ILogger _logger;

    public DiskSpaceCommand(DiskSpaceRequest request, IReportingContainer? container, ILogger logger)
    {
        Request = request;
        _container = container;
        _logger = logger;
    }

    public DiskSpaceRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        dynamic fs = service.GetService();
        OperationResult result = fs.GetFreeSpace(Request.Path);
        _logger.LogInformation(result.Payload.GetValueOrDefault().GetRawText());
        if (result.Payload.HasValue)
        {
            _container?.AddReport("disk-free", result.Payload.Value);
        }
        return Task.FromResult(result);
    }
}

