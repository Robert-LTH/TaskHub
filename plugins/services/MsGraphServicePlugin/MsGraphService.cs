using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace MsGraphServicePlugin;

public class MsGraphServicePlugin : IServicePlugin
{
    private readonly IHttpClientFactory _factory;

    public MsGraphServicePlugin(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public string Name => "msgraph";

    public async Task<string> GetAsync(string resource, CancellationToken cancellationToken)
    {
        var client = _factory.CreateClient("msgraph");
        var endpoint = $"https://graph.microsoft.com/v1.0/{resource}";
        return await client.GetStringAsync(endpoint, cancellationToken);
    }
}
