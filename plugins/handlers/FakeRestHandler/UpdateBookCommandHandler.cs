using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;
using FakeRestServicePlugin;

namespace FakeRestHandler;

public class UpdateBookCommandHandler : CommandHandlerBase, ICommandHandler<UpdateBookCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "book-update" };
    public override string ServiceName => "fakerest";

    UpdateBookCommand ICommandHandler<UpdateBookCommand>.Create(JsonElement payload)
    {
        var book = JsonSerializer.Deserialize<Book>(payload.GetRawText()) ?? new Book();
        return new UpdateBookCommand(book);
    }

    public override ICommand Create(JsonElement payload) => ((ICommandHandler<UpdateBookCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}



