using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;
using FakeRestServicePlugin;

namespace FakeRestHandler;

public class UpdateBookCommandHandler : CommandHandlerBase, ICommandHandler<UpdateBookCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "book-update" };
    public override string ServiceName => "fakerest";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;

    UpdateBookCommand ICommandHandler<UpdateBookCommand>.Create(JsonElement payload, ILogger logger)
    {
        var book = JsonSerializer.Deserialize<Book>(payload.GetRawText()) ?? new Book();
        return new UpdateBookCommand(book);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) => ((ICommandHandler<UpdateBookCommand>)this).Create(payload, logger);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}


