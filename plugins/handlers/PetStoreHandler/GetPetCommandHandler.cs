using System;
using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace PetStoreHandler;

public class GetPetCommandHandler : CommandHandlerBase, ICommandHandler<GetPetCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "pet-get" };
    public override string ServiceName => "petstore";

    GetPetCommand ICommandHandler<GetPetCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<GetPetRequest>(payload.GetRawText()) ?? new GetPetRequest();
        return new GetPetCommand(request);
    }

    public override ICommand Create(JsonElement payload) => ((ICommandHandler<GetPetCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        //throw new NotImplementedException();
    }
}
