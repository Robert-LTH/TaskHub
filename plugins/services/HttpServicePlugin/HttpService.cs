using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace HttpServicePlugin;

public class HttpServicePlugin : IServicePlugin
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<HttpServicePlugin> _logger;

    public HttpServicePlugin(ILogger<HttpServicePlugin> logger)
    {
        _logger = logger;
        var services = new ServiceCollection();
        services.AddHttpClient("http").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseDefaultCredentials = true
        });
        var provider = services.BuildServiceProvider();
        _factory = provider.GetRequiredService<IHttpClientFactory>();
    }

    public string Name => "http";

    public async Task<string> GetAsync(string resource, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Requesting {Resource}", resource);
        var client = _factory.CreateClient("http");
        return await client.GetStringAsync(resource, cancellationToken);
    }
}
