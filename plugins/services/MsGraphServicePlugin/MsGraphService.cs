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
        var cert = OperatingSystem.IsWindows() ?
            new X509Certificate2(certPath) :
            new X509Certificate2(certPath, certPassword);
        _credential = new ClientCertificateCredential(tenantId, clientId, cert);

        var services = new ServiceCollection();
        services.AddHttpClient("msgraph");
        var provider = services.BuildServiceProvider();
        _factory = provider.GetRequiredService<IHttpClientFactory>();
    }

    public string Name => "msgraph";

    public async Task<string> GetAsync(string resource, CancellationToken cancellationToken)
    {
        var token = await _credential.GetTokenAsync(new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }), cancellationToken);
        var client = _factory.CreateClient("msgraph");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        var endpoint = $"https://graph.microsoft.com/v1.0/{resource}";
        _logger.LogInformation("Requesting {Endpoint}", endpoint);
        return await client.GetStringAsync(endpoint, cancellationToken);
    }
}
