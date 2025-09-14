using System;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Abstractions;

namespace BinanceServicePlugin;

public class BinanceServicePlugin : IServicePlugin, IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly BinanceClient _client;

    public BinanceServicePlugin()
    {
        var services = new ServiceCollection();
        services.AddHttpClient<BinanceClient>();
        _provider = services.BuildServiceProvider();
        _client = _provider.GetRequiredService<BinanceClient>();
    }

    public string Name => "binance";

    public object GetService() => _client;

    public void Dispose()
    {
        _provider.Dispose();
        GC.SuppressFinalize(this);
    }
}
