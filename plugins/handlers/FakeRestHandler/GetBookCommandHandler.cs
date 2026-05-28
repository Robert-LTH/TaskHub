using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace FakeRestHandler;

public class GetBookCommandHandler : CommandHandlerBase, ICommandHandler<GetBookCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "book-get" };
    public override string ServiceName => "fakerest";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;

    GetBookCommand ICommandHandler<GetBookCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<GetBookRequest>(payload.GetRawText()) ?? new GetBookRequest();
        return new GetBookCommand(request);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) => ((ICommandHandler<GetBookCommand>)this).Create(payload, logger);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
        //throw new NotImplementedException();
    }
}
