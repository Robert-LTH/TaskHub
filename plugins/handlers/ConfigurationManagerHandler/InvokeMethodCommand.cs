using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace ConfigurationManagerHandler;

public class InvokeMethodCommand : ICommand
{
    private readonly bool _useAdminService;

    public InvokeMethodCommand(InvokeMethodRequest request, bool useAdminService)
    {
        Request = request;
        _useAdminService = useAdminService;
    }

    public InvokeMethodRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        if (_useAdminService)
        {
            return Task.FromResult(new OperationResult(null, "InvokeMethod not supported when using admin service"));
        }

        dynamic wmi = service.GetService();
        return Task.FromResult((OperationResult)wmi.InvokeMethod(
            Request.Host ?? ".",
            Request.Namespace ?? "root\\cimv2",
            Request.Path ?? string.Empty,
            Request.Method ?? string.Empty,
            Request.Parameters ?? new Dictionary<string, object?>()));
    }
}
