using System.Collections.Generic;

namespace ConfigurationManagerHandler;

public class InvokeMethodRequest
{
    public string? Host { get; set; }
    public string? Namespace { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public Dictionary<string, object?>? Parameters { get; set; }
}
