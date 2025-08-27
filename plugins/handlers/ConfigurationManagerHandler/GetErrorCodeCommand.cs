using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace ConfigurationManagerHandler;

public class GetErrorCodeCommand : ICommand
{
    private readonly bool _useAdminService;

    public GetErrorCodeCommand(GetErrorCodeRequest request, bool useAdminService)
    {
        Request = request;
        _useAdminService = useAdminService;
    }

    public GetErrorCodeRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        if (_useAdminService)
        {
            return new OperationResult(null, "GetErrorCode not supported when using admin service");
        }

        dynamic wmi = service.GetService();
        return wmi.GetErrorCode(
            Request.Host ?? ".",
            Request.Namespace ?? "root\\cimv2",
            Request.Class ?? "Win32_PnPEntity",
            Request.PnpDeviceId ?? string.Empty);
    }
}
