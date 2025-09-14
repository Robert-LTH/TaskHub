using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using FakeRestServicePlugin;

namespace FakeRestHandler;

public class ListBooksCommand : ICommand
{
    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (FakeRestClient)service.GetService();
        var books = await client.GetBooksAsync(cancellationToken);
        var element = JsonSerializer.SerializeToElement(books);
        return new OperationResult(element, "success");
    }
}
