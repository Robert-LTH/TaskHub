namespace HyperVHandler;

public class CreateVSwitchRequest
{
    public string Name { get; set; } = string.Empty;
    public string SwitchType { get; set; } = "Internal";
}
