using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace HttpServicePlugin;

public class HttpServicePlugin : IServicePlugin
{
    public string Name => "http";

    public async Task<string> GetAsync(string resource, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        return await client.GetStringAsync(resource, cancellationToken);
    }
}
