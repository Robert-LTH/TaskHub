using System.Collections.Generic;
using TaskHub.Abstractions;

namespace TaskHub.Server;

public record CommandInput(string Name, string Type);

public record CommandInfo(
    string Name,
    string Service,
    CommandExecutionContext ExecutionContext,
    IReadOnlyList<CommandInput> Inputs);
