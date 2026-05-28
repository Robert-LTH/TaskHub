using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace FakeRestHandler;

public class DeleteBookCommandHandler : CommandHandlerBase, ICommandHandler<DeleteBookCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "book-delete" };
    public override string ServiceName => "fakerest";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;

    DeleteBookCommand ICommandHandler<DeleteBookCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<GetBookRequest>(payload.GetRawText()) ?? new GetBookRequest();
        return new DeleteBookCommand(request);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) => ((ICommandHandler<DeleteBookCommand>)this).Create(payload, logger);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}


