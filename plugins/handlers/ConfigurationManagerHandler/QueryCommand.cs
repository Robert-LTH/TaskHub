using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace ConfigurationManagerHandler;

public class QueryCommand : ICommand
{
    private readonly bool _useAdminService;

    public QueryCommand(QueryRequest request, bool useAdminService)
    {
        Request = request;
        _useAdminService = useAdminService;
    }

    public QueryRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        if (_useAdminService)
        {
            var admin = service.GetService();
            var mi = admin.GetType().GetMethod(
                "Get",
                new[] { typeof(string), typeof(string), typeof(System.Threading.CancellationToken) });
            if (mi == null)
            {
                throw new MissingMethodException("Admin service does not implement expected Get method");
            }
            var task = (System.Threading.Tasks.Task<OperationResult>)mi.Invoke(admin, new object[]
            {
                Request.BaseUrl ?? string.Empty,
                Request.Resource ?? string.Empty,
                cancellationToken
            })!;
            return await task.ConfigureAwait(false);
        }
        else
        {
            var wmi = service.GetService();
            var mi = wmi.GetType().GetMethod(
                "Query",
                new[] { typeof(string), typeof(string), typeof(string) });
            if (mi == null)
            {
                throw new MissingMethodException("WMI service does not implement expected Query method");
            }
            return (OperationResult)mi.Invoke(wmi, new object[]
            {
                Request.Host ?? ".",
                Request.Namespace ?? "root\\cimv2",
                Request.Query ?? string.Empty
            })!;
        }
    }
}
