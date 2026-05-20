using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using TaskHub.Abstractions;

namespace BitLockerHandler;

public class BitLockerCommandHandler : CommandHandlerBase, ICommandHandler<RotateKeyCommand>
{
    private static readonly SemaphoreSlim ConnectionLock = new(1, 1);
    private static HubConnection? _connection;
    private static string? _hubUrl;

    public override IReadOnlyCollection<string> Commands => new[] { "bitlocker-rotate" };
    public override string ServiceName => "bitlocker";

    RotateKeyCommand ICommandHandler<RotateKeyCommand>.Create(JsonElement payload)
    {
        var request = JsonSerializer.Deserialize<RotateKeyRequest>(payload.GetRawText()) ?? new RotateKeyRequest();
        return new RotateKeyCommand(request);
    }

    public override ICommand Create(JsonElement payload) =>
        ((ICommandHandler<RotateKeyCommand>)this).Create(payload);

    public override void OnLoaded(IServiceProvider services)
    {
        base.OnLoaded(services);
        var config = services.GetService(typeof(IConfiguration)) as IConfiguration;
        _hubUrl = config?["PluginSettings:BitLocker:HubUrl"];
    }

    internal static async Task ReportKeyAsync(string deviceId, string key)
    {
        var connection = await GetConnectionAsync();
        if (connection?.State == HubConnectionState.Connected)
        {
            await connection.SendAsync("ReportKey", deviceId, key);
        }
    }

    private static async Task<HubConnection?> GetConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(_hubUrl))
        {
            return null;
        }

        if (_connection?.State == HubConnectionState.Connected)
        {
            return _connection;
        }

        await ConnectionLock.WaitAsync();
        try
        {
            _connection ??= new HubConnectionBuilder().WithUrl(_hubUrl).Build();
            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync();
            }
            return _connection;
        }
        catch
        {
            return null;
        }
        finally
        {
            ConnectionLock.Release();
        }
    }
}
