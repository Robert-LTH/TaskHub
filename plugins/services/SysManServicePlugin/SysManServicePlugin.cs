using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskHub.Abstractions;

namespace SysManServicePlugin;

public sealed class SysManServicePlugin : IServicePlugin, IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly SysManClient _client;

    public SysManServicePlugin(IConfiguration configuration)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var services = new ServiceCollection();
        services.AddSingleton(CreateOptions(configuration));
        services.AddHttpClient<SysManClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<SysManClientOptions>();
            http.BaseAddress = options.BaseAddress ?? new Uri("https://localhost/");
        });

        _provider = services.BuildServiceProvider();
        _client = _provider.GetRequiredService<SysManClient>();
    }

    public string Name => "sysman";

    public object GetService() => _client;

    public void Dispose()
    {
        _provider.Dispose();
        GC.SuppressFinalize(this);
    }

    private static SysManClientOptions CreateOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("PluginSettings:SysMan");
        var baseAddress = section["BaseAddress"];

        Uri? uri = null;
        if (!string.IsNullOrWhiteSpace(baseAddress) && Uri.TryCreate(baseAddress, UriKind.Absolute, out var parsed))
        {
            uri = parsed;
        }

        return new SysManClientOptions
        {
            BaseAddress = uri ?? new Uri("https://localhost/")
        };
    }
}

internal sealed class SysManClientOptions
{
    public Uri? BaseAddress { get; init; }
}
