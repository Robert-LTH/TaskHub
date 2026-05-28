using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace PetStoreHandler;

public class GetPetCommandHandler : CommandHandlerBase, ICommandHandler<GetPetCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "pet-get" };
    public override string ServiceName => "petstore";
    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUserOrSystem;

    GetPetCommand ICommandHandler<GetPetCommand>.Create(JsonElement payload, ILogger logger)
    {
        var request = JsonSerializer.Deserialize<GetPetRequest>(payload.GetRawText()) ?? new GetPetRequest();
        return new GetPetCommand(request);
    }

    public override ICommand Create(JsonElement payload, ILogger logger) => ((ICommandHandler<GetPetCommand>)this).Create(payload, logger);

    public override void OnLoaded(IServiceProvider services)
    {
        //throw new NotImplementedException();
    }
}
