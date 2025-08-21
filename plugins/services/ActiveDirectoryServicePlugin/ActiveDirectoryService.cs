using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace ActiveDirectoryServicePlugin;

public class ActiveDirectoryServicePlugin : IServicePlugin
{
    public string Name => "activedirectory";

    public async Task<string> GetAsync(string resource, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var endpoint = $"https://graph.windows.net/{resource}?api-version=1.6";
        return await client.GetStringAsync(endpoint, cancellationToken);
    }
}
