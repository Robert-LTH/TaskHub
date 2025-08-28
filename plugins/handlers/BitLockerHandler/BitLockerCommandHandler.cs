using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using BitLockerServicePlugin;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace BitLockerHandler;

public class BitLockerCommandHandler : CommandHandlerBase, ICommandHandler<RotateKeyCommand>
{
    private static HubConnection? _connection;
    private static BitLockerService? _service;

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
        var logger = services.GetRequiredService<ILogger<BitLockerService>>();
        var config = services.GetService<IConfiguration>();
        var url = config?.GetValue<string>("PluginSettings:BitLocker:HubUrl") ?? "http://localhost/bitlocker";
        _connection = new HubConnectionBuilder().WithUrl(url).Build();
        _ = _connection.StartAsync();

        _service = new BitLockerService(logger);
        _service.KeyAvailable += async (device, key) =>
        {
            await ReportKeyAsync(device, key);
        };
    }

    internal static async Task ReportKeyAsync(string deviceId, string key)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.SendAsync("ReportKey", deviceId, key);
        }
    }
}
