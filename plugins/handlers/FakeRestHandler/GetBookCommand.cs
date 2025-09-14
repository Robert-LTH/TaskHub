using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using FakeRestServicePlugin;

namespace FakeRestHandler;

public class GetBookCommand : ICommand
{
    public GetBookCommand(GetBookRequest request)
    {
        Request = request;
    }

    public GetBookRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (FakeRestClient)service.GetService();
        var book = await client.GetBookAsync(Request.Id, cancellationToken);
        var element = JsonSerializer.SerializeToElement(book);
        var status = book != null ? "success" : "not_found";
        return new OperationResult(element, status);
    }
}
