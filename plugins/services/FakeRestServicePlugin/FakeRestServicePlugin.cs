using System;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Abstractions;

namespace FakeRestServicePlugin;

public class FakeRestServicePlugin : IServicePlugin, IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly FakeRestClient _client;

    public FakeRestServicePlugin()
    {
        var services = new ServiceCollection();
        services.AddHttpClient<FakeRestClient>();
        _provider = services.BuildServiceProvider();
        _client = _provider.GetRequiredService<FakeRestClient>();
    }

    public string Name => "fakerest";

    public object GetService() => _client;

    public void Dispose()
    {
        _provider.Dispose();
        GC.SuppressFinalize(this);
    }
}
