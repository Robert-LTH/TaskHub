namespace HyperVHandler;

public class CreateVhdxRequest
{
    public string Path { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool Dynamic { get; set; } = true;
}
