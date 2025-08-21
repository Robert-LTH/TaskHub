using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace MsGraphServicePlugin;

public class MsGraphServicePlugin : IServicePlugin
{
    private readonly IHttpClientFactory _factory;
    private readonly ClientCertificateCredential _credential;
    private readonly ILogger<MsGraphServicePlugin> _logger;

    public MsGraphServicePlugin(IConfiguration config, ILogger<MsGraphServicePlugin> logger)
    {
        _logger = logger;
        var section = config.GetSection("PluginSettings:MsGraph");
        var tenantId = section["TenantId"];
        var clientId = section["ClientId"];
        var certPath = section["CertificatePath"];
        var certPassword = section["CertificatePassword"];
        var cert = OperatingSystem.IsWindows()
            ? new X509Certificate2(certPath)
            : new X509Certificate2(certPath, certPassword);
        _credential = new ClientCertificateCredential(tenantId, clientId, cert);

        var services = new ServiceCollection();
        services.AddTransient<AuthHandler>(_ => new AuthHandler(_credential, _logger));
        services.AddHttpClient("msgraph")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseDefaultCredentials = true
            })
            .AddHttpMessageHandler<AuthHandler>();
        var provider = services.BuildServiceProvider();
        _factory = provider.GetRequiredService<IHttpClientFactory>();
    }

    public string Name => "msgraph";

    public object GetService() => _factory.CreateClient("msgraph");

    private class AuthHandler : DelegatingHandler
    {
        private readonly ClientCertificateCredential _credential;
        private readonly ILogger _logger;

        public AuthHandler(ClientCertificateCredential credential, ILogger logger)
        {
            _credential = credential;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _credential.GetTokenAsync(new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }), cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            _logger.LogInformation("Requesting {Endpoint}", request.RequestUri);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

