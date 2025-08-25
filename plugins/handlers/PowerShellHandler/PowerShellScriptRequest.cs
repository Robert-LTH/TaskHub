using System.Collections.Generic;

namespace PowerShellHandler;

public class PowerShellScriptRequest
{
    public string Script { get; set; } = string.Empty;
    public string? Version { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}
