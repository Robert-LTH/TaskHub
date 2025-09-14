using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using FakeRestServicePlugin;

namespace FakeRestHandler;

public class UpdateBookCommand : ICommand
{
    public UpdateBookCommand(Book book)
    {
        Book = book;
    }

    public Book Book { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var client = (FakeRestClient)service.GetService();
        var success = await client.UpdateBookAsync(Book.Id, Book, cancellationToken);
        var element = JsonSerializer.SerializeToElement(success);
        var status = success ? "success" : "error";
        return new OperationResult(element, status);
    }
}
