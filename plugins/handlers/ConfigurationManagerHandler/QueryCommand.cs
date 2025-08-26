using System.Threading;
using System.Threading.Tasks;
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

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        if (_useAdminService)
        {
            dynamic admin = service.GetService();
            return await admin.Get(Request.BaseUrl ?? string.Empty, Request.Resource ?? string.Empty, cancellationToken);
        }
        else
        {
            dynamic wmi = service.GetService();
            return wmi.Query(Request.Host ?? ".", Request.Namespace ?? "root\\cimv2", Request.Query ?? string.Empty);
        }
    }
}
