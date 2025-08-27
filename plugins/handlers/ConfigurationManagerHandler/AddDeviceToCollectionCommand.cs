using System;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace ConfigurationManagerHandler;

public class AddDeviceToCollectionCommand : ICommand
{
    private readonly bool _useAdminService;

    public AddDeviceToCollectionCommand(AddDeviceToCollectionRequest request, bool useAdminService)
    {
        Request = request;
        _useAdminService = useAdminService;
    }

    public AddDeviceToCollectionRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        var ids = Request.DeviceIds ?? Array.Empty<string>();
        if (_useAdminService)
        {
            dynamic admin = service.GetService();
            return await admin.AddDeviceToCollection(
                Request.BaseUrl ?? string.Empty,
                Request.CollectionId ?? string.Empty,
                ids,
                cancellationToken);
        }
        else
        {
            dynamic wmi = service.GetService();
            return wmi.AddDeviceToCollection(
                Request.Host ?? ".",
                Request.Namespace ?? "root\\cimv2",
                Request.CollectionId ?? string.Empty,
                ids);
        }
    }
}
