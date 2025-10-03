using System;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Abstractions;

namespace PetStoreServicePlugin;

public class PetStoreServicePlugin : IServicePlugin, IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly PetStoreClient _client;

    public IServiceProvider Services { get; private set; } = default!;

    public PetStoreServicePlugin()
    {
        var services = new ServiceCollection();
        services.AddHttpClient<PetStoreClient>();
        _provider = services.BuildServiceProvider();
        _client = _provider.GetRequiredService<PetStoreClient>();
    }

    public string Name => "petstore";

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

