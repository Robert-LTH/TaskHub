namespace ConfigurationManagerHandler;

public class AddDeviceToCollectionRequest
{
    public string? BaseUrl { get; set; }
    public string? Host { get; set; }
    public string? Namespace { get; set; }
    public string? CollectionId { get; set; }
    public string[]? DeviceIds { get; set; }
}
