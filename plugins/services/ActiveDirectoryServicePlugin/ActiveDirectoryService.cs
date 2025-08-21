using System.DirectoryServices;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace ActiveDirectoryServicePlugin;

public class ActiveDirectoryServicePlugin : IServicePlugin
{
    public string Name => "activedirectory";

    public async Task<string> GetAsync(string resource, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            using var entry = new DirectoryEntry($"LDAP://{resource}");
            using var searcher = new DirectorySearcher(entry)
            {
                SearchScope = SearchScope.Subtree,
                SizeLimit = 1
            };

            var result = searcher.FindOne();
            return result?.Path ?? string.Empty;
        }, cancellationToken);
    }
}
