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

    public HttpServicePlugin(ILogger<HttpServicePlugin> logger)
    {
        var services = new ServiceCollection();
        services.AddTransient<LoggingHandler>(_ => new LoggingHandler(logger));
        services.AddHttpClient("http")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseDefaultCredentials = true
            })
            .AddHttpMessageHandler<LoggingHandler>();
        var provider = services.BuildServiceProvider();
        _factory = provider.GetRequiredService<IHttpClientFactory>();
    }

    public string Name => "http";

    public object GetService() => _factory.CreateClient("http");

    private class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger _logger;

        public LoggingHandler(ILogger logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Requesting {Resource}", request.RequestUri);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

