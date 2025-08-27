namespace ConfigurationManagerHandler;

public class GetErrorCodeRequest
{
    public string? Host { get; set; }
    public string? Namespace { get; set; }
    public string? Class { get; set; }
    public string? PnpDeviceId { get; set; }
}
