using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;
using FakeRestServicePlugin;

namespace FakeRestHandler;

public class CreateBookCommandHandler : CommandHandlerBase, ICommandHandler<CreateBookCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "book-create" };
    public override string ServiceName => "fakerest";

    CreateBookCommand ICommandHandler<CreateBookCommand>.Create(JsonElement payload)
    {
        var book = JsonSerializer.Deserialize<Book>(payload.GetRawText()) ?? new Book();
        return new CreateBookCommand(book);
    }

    public override ICommand Create(JsonElement payload) => ((ICommandHandler<CreateBookCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}



