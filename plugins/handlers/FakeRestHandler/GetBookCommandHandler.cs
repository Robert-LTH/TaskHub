using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace FakeRestHandler;

public class GetBookCommandHandler : CommandHandlerBase, ICommandHandler<GetBookCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "book-get" };
    public override string ServiceName => "fakerest";

    GetBookCommand ICommandHandler<GetBookCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<GetBookRequest>(payload.GetRawText()) ?? new GetBookRequest();
        return new GetBookCommand(request);
    }

    public override ICommand Create(JsonElement payload) => ((ICommandHandler<GetBookCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
        //throw new NotImplementedException();
    }
}

