using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using FakeRestServicePlugin;

namespace FakeRestHandler;

public class DeleteBookCommand : ICommand
{
    public DeleteBookCommand(GetBookRequest request)
    {
        Request = request;
    }

    public GetBookRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (FakeRestClient)service.GetService();
        var success = await client.DeleteBookAsync(Request.Id, cancellationToken);
        var element = JsonSerializer.SerializeToElement(success);
        var status = success ? "success" : "not_found";
        return new OperationResult(element, status);
    }
}
