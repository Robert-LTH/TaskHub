using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Abstractions;

namespace OverviewServicePlugin;

public sealed class OverviewServicePlugin : IServicePlugin, IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly OverviewApiClient _client;

    public IServiceProvider Services { get; private set; } = default!;

    public OverviewServicePlugin()
    {
        var services = new ServiceCollection();
        services
            .AddHttpClient<OverviewApiClient>(client =>
            {
                if (client.BaseAddress == null)
                {
                    client.BaseAddress = new Uri("https://posbeta.eklientref.se/overview/");
                }
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseDefaultCredentials = true
            });

        _provider = services.BuildServiceProvider();
        _client = _provider.GetRequiredService<OverviewApiClient>();
    }

    public string Name => "overview";

    public void OnLoaded(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public object GetService() => _client;

    public void Dispose()
    {
        _provider.Dispose();
        GC.SuppressFinalize(this);
    }
}


