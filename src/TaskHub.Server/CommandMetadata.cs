using System.Collections.Generic;

namespace TaskHub.Server;

public record CommandInput(string Name, string Type);

public record CommandInfo(string Name, string Service, IReadOnlyList<CommandInput> Inputs);
