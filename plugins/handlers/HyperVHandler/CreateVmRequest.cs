namespace HyperVHandler;

public class CreateVmRequest
{
    public string Name { get; set; } = string.Empty;
    public string VhdPath { get; set; } = string.Empty;
    public string SwitchName { get; set; } = string.Empty;
    public long MemoryStartupBytes { get; set; } = 536870912;
}
