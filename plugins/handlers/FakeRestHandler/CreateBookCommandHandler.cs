using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using FakeRestServicePlugin;

namespace FakeRestHandler;

public class CreateBookCommandHandler : CommandHandlerBase, ICommandHandler<CreateBookCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "book-create" };
    public override string ServiceName => "fakerest";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;

    CreateBookCommand ICommandHandler<CreateBookCommand>.Create(JsonElement payload, ILogger logger)
    {
        var book = JsonSerializer.Deserialize<Book>(payload.GetRawText()) ?? new Book();
        return new CreateBookCommand(book);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) => ((ICommandHandler<CreateBookCommand>)this).Create(payload, logger);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}


