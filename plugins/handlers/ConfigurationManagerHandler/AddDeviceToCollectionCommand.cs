using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var ids = Request.DeviceIds ?? Array.Empty<string>();
        if (_useAdminService)
        {
            var admin = service.GetService();
            var mi = admin.GetType().GetMethod(
                "AddDeviceToCollection",
                new[] { typeof(string), typeof(string), typeof(string[]), typeof(CancellationToken) });
            if (mi == null)
            {
                throw new MissingMethodException("Admin service does not implement expected AddDeviceToCollection method");
            }
            var task = (Task<OperationResult>)mi.Invoke(admin, new object[]
            {
                Request.BaseUrl ?? string.Empty,
                Request.CollectionId ?? string.Empty,
                ids,
                cancellationToken
            })!;
            return await task.ConfigureAwait(false);
        }
        else
        {
            var wmi = service.GetService();
            var mi = wmi.GetType().GetMethod(
                "AddDeviceToCollection",
                new[] { typeof(string), typeof(string), typeof(string), typeof(string[]) });
            if (mi == null)
            {
                return new OperationResult(null, "AddDeviceToCollection not supported by service");
            }
            return (OperationResult)mi.Invoke(wmi, new object[]
            {
                Request.Host ?? ".",
                Request.Namespace ?? "root\\cimv2",
                Request.CollectionId ?? string.Empty,
                ids
            })!;
        }
    }
}
