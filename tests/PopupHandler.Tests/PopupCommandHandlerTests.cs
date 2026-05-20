using System;
using System.Drawing;
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
        Assert.Equal(string.Empty, handler.ServiceName);
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
    public void NormalizeAppliesDefaultsAndBounds()
    {
        var options = ShowPopupCommand.Normalize(new ShowPopupRequest
        {
            Title = "",
            Message = "Task finished",
            DurationMilliseconds = 10,
            Width = 1000,
            Height = 10,
            Margin = 500
        });

        Assert.Equal("TaskHub", options.Title);
        Assert.Equal("Task finished", options.Message);
        Assert.Equal(500, options.DurationMilliseconds);
        Assert.Equal(800, options.Width);
        Assert.Equal(100, options.Height);
        Assert.Equal(120, options.Margin);
    }

    [Fact]
    public void CalculateLowerRightLocationUsesWorkingAreaBottomRight()
    {
        var location = ShowPopupCommand.CalculateLowerRightLocation(
            new Rectangle(10, 20, 1000, 800),
            width: 360,
            height: 140,
            margin: 16);

        Assert.Equal(new Point(634, 664), location);
    }

    [Fact]
    public async Task ExecuteRequiresMessage()
    {
        var command = new ShowPopupCommand(new ShowPopupRequest());

        var result = await command.ExecuteAsync(new FakeServicePlugin(), NullLogger.Instance, CancellationToken.None);

        Assert.Null(result.Payload);
        Assert.Equal("message is required", result.Result);
    }

    private sealed class FakeServicePlugin : IServicePlugin
    {
        public string Name => string.Empty;

        public IServiceProvider Services { get; private set; } = default!;

        public void OnLoaded(IServiceProvider services)
        {
            Services = services;
        }

        public object GetService() => AppDomain.CurrentDomain;
    }
}
