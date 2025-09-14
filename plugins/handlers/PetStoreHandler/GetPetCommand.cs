using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using PetStoreServicePlugin;

namespace PetStoreHandler;

public class GetPetCommand : ICommand
{
    public GetPetCommand(GetPetRequest request)
    {
        Request = request;
    }

    public GetPetRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (PetStoreClient)service.GetService();
        var pet = await client.GetPetAsync(Request.Id, cancellationToken);
        var element = JsonSerializer.SerializeToElement(pet);
        var status = pet != null ? "success" : "not_found";
        return new OperationResult(element, status);
    }
}
