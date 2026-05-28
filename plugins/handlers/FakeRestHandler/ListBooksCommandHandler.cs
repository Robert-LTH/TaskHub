using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace FakeRestHandler;

public class ListBooksCommandHandler : CommandHandlerBase, ICommandHandler<ListBooksCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "book-list" };
    public override string ServiceName => "fakerest";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;

    ListBooksCommand ICommandHandler<ListBooksCommand>.Create(JsonElement payload, ILogger logger)
    {
        return new ListBooksCommand();
    }

    public override ICommand Create(JsonElement payload, ILogger logger) => ((ICommandHandler<ListBooksCommand>)this).Create(payload, logger);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
    }
}


