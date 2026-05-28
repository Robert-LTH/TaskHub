using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace ConfigurationManagerHandler;

public class InvokeMethodCommand : ICommand
{
    private readonly bool _useAdminService;
    private readonly ILogger _logger;


    public InvokeMethodCommand(InvokeMethodRequest request, bool useAdminService, ILogger logger)
    {
        Request = request;
        _useAdminService = useAdminService;
        _logger = logger;
    }

    public InvokeMethodRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        if (_useAdminService)
        {
            return Task.FromResult(new OperationResult(null, "InvokeMethod not supported when using admin service"));
        }

        var wmi = service.GetService();
        var mi = wmi.GetType().GetMethod(
            "InvokeMethod",
            new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(Dictionary<string, object?>) });
        if (mi == null)
        {
            return Task.FromResult(new OperationResult(null, "InvokeMethod not supported by service"));
        }
        var result = (OperationResult)mi.Invoke(wmi, new object[]
        {
            Request.Host ?? ".",
            Request.Namespace ?? "root\\cimv2",
            Request.Path ?? string.Empty,
            Request.Method ?? string.Empty,
            Request.Parameters ?? new Dictionary<string, object?>()
        })!;
        return Task.FromResult(result);
    }
}
