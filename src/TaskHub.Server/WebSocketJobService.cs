using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TaskHub.Server;

public class WebSocketJobService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebSocketJobService> _logger;
    private readonly IBackgroundJobClient _client;

    public WebSocketJobService(IConfiguration configuration, ILogger<WebSocketJobService> logger, IBackgroundJobClient client)
    {
        _configuration = configuration;
        _logger = logger;
        _client = client;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var url = _configuration["JobHandling:WebSocketServerUrl"];
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("WebSocket server URL is not configured.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            using var socket = new ClientWebSocket();
            try
            {
                await socket.ConnectAsync(new Uri(url), stoppingToken);
                await ReceiveLoop(socket, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket connection failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ReceiveLoop(ClientWebSocket socket, CancellationToken token)
    {
        var buffer = new byte[8192];
        var builder = new StringBuilder();

        while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(buffer, token);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", token);
                break;
            }

            builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (result.EndOfMessage)
            {
                var message = builder.ToString();
                builder.Clear();

                try
                {
                    var request = JsonSerializer.Deserialize<CommandChainRequest>(message);
                    if (request != null)
                    {
                        _client.Enqueue<CommandExecutor>(exec => exec.ExecuteChain(request.Commands, request.Payload, null!, CancellationToken.None));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process job message {Message}", message);
                }
            }
        }
    }
}

