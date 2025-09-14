using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using FakeRestServicePlugin;

namespace FakeRestHandler;

public class CreateBookCommand : ICommand
{
    public CreateBookCommand(Book book)
    {
        Book = book;
    }

    public Book Book { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (FakeRestClient)service.GetService();
        var created = await client.CreateBookAsync(Book, cancellationToken);
        var element = JsonSerializer.SerializeToElement(created);
        var status = created != null ? "success" : "error";
        return new OperationResult(element, status);
    }
}
