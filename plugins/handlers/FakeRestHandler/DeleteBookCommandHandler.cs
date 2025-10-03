using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace FakeRestHandler;

public class DeleteBookCommandHandler : CommandHandlerBase, ICommandHandler<DeleteBookCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "book-delete" };
    public override string ServiceName => "fakerest";

    DeleteBookCommand ICommandHandler<DeleteBookCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<GetBookRequest>(payload.GetRawText()) ?? new GetBookRequest();
        return new DeleteBookCommand(request);
    }

    public override ICommand Create(JsonElement payload) => ((ICommandHandler<DeleteBookCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}



