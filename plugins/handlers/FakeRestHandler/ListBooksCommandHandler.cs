using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace FakeRestHandler;

public class ListBooksCommandHandler : CommandHandlerBase, ICommandHandler<ListBooksCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "book-list" };
    public override string ServiceName => "fakerest";

    ListBooksCommand ICommandHandler<ListBooksCommand>.Create(JsonElement payload)
    {
        return new ListBooksCommand();
    }

    public override ICommand Create(JsonElement payload) => ((ICommandHandler<ListBooksCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services) { }
}
