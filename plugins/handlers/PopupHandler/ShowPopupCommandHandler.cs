using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace PopupHandler;

public class ShowPopupCommandHandler : CommandHandlerBase, ICommandHandler<ShowPopupCommand>
{
    public override IReadOnlyCollection<string> Commands => new[] { "popup-show", "show-popup" };

    public override string ServiceName => string.Empty;

    public override CommandExecutionContext ExecutionContext => CommandExecutionContext.RegularUser;

    ShowPopupCommand ICommandHandler<ShowPopupCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<ShowPopupRequest>(payload.GetRawText()) ?? new ShowPopupRequest();
        return new ShowPopupCommand(request);
    }

    public override ICommand Create(JsonElement payload) =>
        ((ICommandHandler<ShowPopupCommand>)this).Create(payload);
}
