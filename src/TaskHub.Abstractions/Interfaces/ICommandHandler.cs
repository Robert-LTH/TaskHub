using System.Collections.Generic;
using System.Text.Json;

namespace TaskHub.Abstractions;

public interface ICommandHandler
{
    IReadOnlyCollection<string> Commands { get; }
    string ServiceName { get; }
    ICommand Create(JsonElement payload);
}

public interface ICommandHandler<out TCommand> : ICommandHandler where TCommand : ICommand
{
    new TCommand Create(JsonElement payload);
}
