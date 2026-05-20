using System.Text.Json.Serialization;

namespace TaskHub.Abstractions;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommandExecutionContext
{
    RegularUser,
    System,
    RegularUserOrSystem
}
