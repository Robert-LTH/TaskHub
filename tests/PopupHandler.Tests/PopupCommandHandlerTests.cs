using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PopupHandler;
using TaskHub.Abstractions;
using Xunit;

namespace PopupHandler.Tests;

public class PopupCommandHandlerTests
{
    [Fact]
    public void CommandsIncludePopupAliases()
    {
        var handler = new ShowPopupCommandHandler();

        Assert.Contains("popup-show", handler.Commands);
        Assert.Contains("show-popup", handler.Commands);
        Assert.Equal("powershell", handler.ServiceName);
        Assert.Equal(CommandExecutionContext.RegularUser, handler.ExecutionContext);
    }

    [Fact]
    public void CreateParsesShowPopupRequest()
    {
        var handler = new ShowPopupCommandHandler();
        var payload = JsonSerializer.SerializeToElement(new
        {
            title = "Deployment",
            message = "Install complete",
            durationMilliseconds = 2500,
            width = 420,
            height = 160,
            margin = 24
        });

        var command = Assert.IsType<ShowPopupCommand>(((ICommandHandler<ShowPopupCommand>)handler).Create(payload));

        Assert.Equal("Deployment", command.Request.Title);
        Assert.Equal("Install complete", command.Request.Message);
        Assert.Equal(2500, command.Request.DurationMilliseconds);
        Assert.Equal(420, command.Request.Width);
        Assert.Equal(160, command.Request.Height);
        Assert.Equal(24, command.Request.Margin);
    }

    [Fact]
    public void BuildPopupScriptPositionsFormAtLowerRight()
    {
        var script = ShowPopupCommand.BuildPopupScript(new ShowPopupRequest
        {
            Title = "O'Hare",
            Message = "Task finished",
            Width = 420,
            Height = 180,
            Margin = 24
        });

        Assert.Contains("Add-Type -ReferencedAssemblies @('System.Windows.Forms', 'System.Drawing')", script);
        Assert.Contains("popupThread.SetApartmentState(ApartmentState.STA)", script);
        Assert.Contains("$popupMargin = 24", script);
        Assert.Contains("workingArea.Right - form.Width - margin", script);
        Assert.Contains("workingArea.Bottom - form.Height - margin", script);
        Assert.Contains("form.TopMost = true", script);
        Assert.Contains("$popupTitle = 'O''Hare'", script);
    }

    [Fact]
    public async Task ExecuteRequiresMessage()
    {
        var command = new ShowPopupCommand(new ShowPopupRequest());

        var result = await command.ExecuteAsync(new FakePowerShellPlugin(), NullLogger.Instance, CancellationToken.None);

        Assert.Null(result.Payload);
        Assert.Equal("message is required", result.Result);
    }

    private sealed class FakePowerShellPlugin : IServicePlugin
    {
        public string Name => "powershell";

        public IServiceProvider Services { get; private set; } = default!;

        public void OnLoaded(IServiceProvider services)
        {
            Services = services;
        }

        public object GetService() => new FakePowerShellService();
    }

    private sealed class FakePowerShellService
    {
        public OperationResult Execute(string scriptBase64, string version = null, Dictionary<string, object> properties = null)
        {
            var script = Encoding.UTF8.GetString(Convert.FromBase64String(scriptBase64));
            var payload = JsonSerializer.SerializeToElement(new
            {
                script
            });

            return new OperationResult(payload, "success");
        }
    }
}
