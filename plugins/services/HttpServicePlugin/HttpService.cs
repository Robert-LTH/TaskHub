using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace HttpServicePlugin;

public class HttpServicePlugin : IServicePlugin, IDisposable
{
    private readonly IHttpClientFactory _factory;
    private readonly ServiceProvider _provider;

    public IServiceProvider Services { get; private set; } = default!;

    public HttpServicePlugin(ILogger<HttpServicePlugin> logger)
    {
        var services = new ServiceCollection();
        services.AddTransient<LoggingHandler>(_ => new LoggingHandler(logger));
        services.AddHttpClient("http")
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (OperatingSystem.IsWindows())
                {
                    handler.UseDefaultCredentials = true;
                }
                return handler;
            })
            .AddHttpMessageHandler<LoggingHandler>();
        _provider = services.BuildServiceProvider();
        _factory = _provider.GetRequiredService<IHttpClientFactory>();
    }

    public string Name => "http";

    public void OnLoaded(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public object GetService() => _factory.CreateClient("http");

    public void Dispose()
    {
        _provider.Dispose();
        GC.SuppressFinalize(this);
    }

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


