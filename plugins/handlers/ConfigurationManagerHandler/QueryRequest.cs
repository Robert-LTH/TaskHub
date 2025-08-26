namespace ConfigurationManagerHandler;

public class QueryRequest
{
    public string? BaseUrl { get; set; }
    public string? Resource { get; set; }
    public string? Host { get; set; }
    public string? Namespace { get; set; }
    public string? Query { get; set; }
}
