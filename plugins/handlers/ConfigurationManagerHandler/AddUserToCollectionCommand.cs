using System;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace ConfigurationManagerHandler;

public class AddUserToCollectionCommand : ICommand
{
    private readonly bool _useAdminService;

    public AddUserToCollectionCommand(AddUserToCollectionRequest request, bool useAdminService)
    {
        Request = request;
        _useAdminService = useAdminService;
    }

    public AddUserToCollectionRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        var ids = Request.UserIds ?? Array.Empty<string>();
        if (_useAdminService)
        {
            dynamic admin = service.GetService();
            return await admin.AddUserToCollection(
                Request.BaseUrl ?? string.Empty,
                Request.CollectionId ?? string.Empty,
                ids,
                cancellationToken);
        }
        else
        {
            dynamic wmi = service.GetService();
            return wmi.AddUserToCollection(
                Request.Host ?? ".",
                Request.Namespace ?? "root\\cimv2",
                Request.CollectionId ?? string.Empty,
                ids);
        }
    }
}
