using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace ProcessHandler;

public class StartProcessCommandHandler : CommandHandlerBase, ICommandHandler<StartProcessCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "process-start", "start-process" };

    public override string ServiceName => "process";

    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;

    StartProcessCommand ICommandHandler<StartProcessCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<StartProcessRequest>(payload.GetRawText()) ?? new StartProcessRequest();
        return new StartProcessCommand(request, logger);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) =>
        ((ICommandHandler<StartProcessCommand>)this).Create(payload, logger);
}
