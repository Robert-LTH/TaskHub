using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace TaskHub.Server;

public class WebSocketJobService : BackgroundService, IResultPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebSocketJobService> _logger;
    private readonly IBackgroundJobClient _client;
    private readonly PayloadVerifier _verifier;
    private readonly CommandExecutor _executor;
    private readonly Channel<string> _sendQueue = Channel.CreateBounded<string>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest });

    public WebSocketJobService(IConfiguration configuration, ILogger<WebSocketJobService> logger, IBackgroundJobClient client, PayloadVerifier verifier, CommandExecutor executor)
    {
        _configuration = configuration;
        _logger = logger;
        _client = client;
        _verifier = verifier;
        _executor = executor;
    }

    public async Task PublishResultAsync(CommandStatusResult result, string? callbackConnectionId, CancellationToken token)
    {
        var envelope = new
        {
            type = "result",
            connectionId = callbackConnectionId,
            result
        };
        var json = JsonSerializer.Serialize(envelope);
        await _sendQueue.Writer.WriteAsync(json, token);
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
                var receiveTask = ReceiveLoop(socket, stoppingToken);
                var sendTask = SendLoop(socket, stoppingToken);
                await Task.WhenAny(receiveTask, sendTask);
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
                        var verifyElement = JsonSerializer.SerializeToElement(request.Commands);
                        if (_verifier.Verify(verifyElement, request.Signature))
                        {
                            var requestedBy = request.RequestedBy;
                            string jobId;
                            var itemsJson = JsonSerializer.Serialize(request.Commands);
                            if (request.Delay.HasValue)
                            {
                                jobId = _client.Schedule<CommandExecutor>(exec => exec.ExecuteChain(itemsJson, requestedBy, null!, CancellationToken.None), request.Delay.Value);
                            }
                            else
                            {
                                jobId = _client.Enqueue<CommandExecutor>(exec => exec.ExecuteChain(itemsJson, requestedBy, null!, CancellationToken.None));
                            }
                            _executor.SetCallback(jobId, request.CallbackConnectionId);
                            _logger.LogInformation("WebSocket user {User} scheduled job {JobId} for commands {Commands}", requestedBy ?? "unknown", jobId, string.Join(", ", System.Linq.Enumerable.Select(request.Commands, c => c.Command)));
                        }
                        else
                        {
                            _logger.LogWarning("Invalid payload signature: {Message}", message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process job message {Message}", message);
                }
            }
        }
    }

    private async Task SendLoop(ClientWebSocket socket, CancellationToken token)
    {
        while (await _sendQueue.Reader.WaitToReadAsync(token) && socket.State == WebSocketState.Open)
        {
            while (_sendQueue.Reader.TryRead(out var message))
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
            }
        }
    }
}

