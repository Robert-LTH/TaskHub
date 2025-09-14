using System.Collections.Generic;
using System.Text.Json;

namespace BinanceHandler;

public class CallEndpointRequest
{
    public string Method { get; set; } = "GET";
    public string Endpoint { get; set; } = string.Empty;
    public Dictionary<string, string>? Query { get; set; }

    public JsonElement? Body { get; set; }
}

