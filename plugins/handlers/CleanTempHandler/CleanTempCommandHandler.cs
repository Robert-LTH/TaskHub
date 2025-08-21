using System.Collections.Generic;
using System.Text.Json;
using TaskHub.Abstractions;

namespace CleanTempHandler;

public class CleanTempCommandHandler : ICommandHandler<CleanTempCommand>
{
    public IReadOnlyCollection<string> Commands => new[] { "clean-temp" };
    public string ServiceName => "filesystem";

    public CleanTempCommand Create(JsonElement payload)
    {
        var path = payload.TryGetProperty("path", out var element) ? element.GetString() : @"C:\\temp22";
        return new CleanTempCommand(path ?? @"C:\\temp22");
    }
}
