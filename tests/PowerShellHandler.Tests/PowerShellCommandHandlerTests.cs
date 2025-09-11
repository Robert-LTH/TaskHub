using PowerShellHandler;
using PowerShellServicePlugin;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Xunit;

namespace PowerShellHandler.Tests;

public class PowerShellCommandHandlerTests
{
    [Fact]
    public void CommandsIncludePowerShellScript()
    {
        var plugin = new PowerShellServicePlugin.PowerShellServicePlugin();
        var handler = new PowerShellCommandHandler();
        Assert.Contains("powershell-script", handler.Commands);
        Assert.Equal("powershell", handler.ServiceName);
    }

    [Fact]
    public async Task ExecutesScript()
    {
        var plugin = new PowerShellServicePlugin.PowerShellServicePlugin();
        var handler = new PowerShellCommandHandler();
        var request = new PowerShellScriptRequest { Script = "Write-Output 5" };
        var payload = JsonSerializer.SerializeToElement(request);

        var result = await handler.ExecuteAsync(payload, plugin, CancellationToken.None);
        Assert.Equal("success", result.Result);
        var element = result.Payload!.Value;
        var first = element.EnumerateArray().First();
        Assert.Equal(5, first.GetInt32());
    }
}
