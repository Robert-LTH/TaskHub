using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace MsGraphServicePlugin;

public class MsGraphServicePlugin : IServicePlugin
{
    public string Name => "msgraph";

    public async Task<string> GetAsync(string resource, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var endpoint = $"https://graph.microsoft.com/v1.0/{resource}";
        return await client.GetStringAsync(endpoint, cancellationToken);
    }
}
