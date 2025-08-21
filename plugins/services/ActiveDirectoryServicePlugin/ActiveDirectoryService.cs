using System.DirectoryServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace ActiveDirectoryServicePlugin;

public class ActiveDirectoryServicePlugin : IServicePlugin
{
    private readonly string? _username;
    private readonly string? _password;
    private readonly bool _useProcessContext;
    private readonly ILogger<ActiveDirectoryServicePlugin> _logger;

    public ActiveDirectoryServicePlugin(IConfiguration config, ILogger<ActiveDirectoryServicePlugin> logger)
    {
        _logger = logger;
        var section = config.GetSection("PluginSettings:ActiveDirectory");
        _useProcessContext = bool.TryParse(section["UseProcessContext"], out var useProcess) && useProcess;
        if (!_useProcessContext)
        {
            _username = section["Username"];
            _password = section["Password"];
        }
    }

=======
    public string Name => "activedirectory";

    public async Task<string> GetAsync(string resource, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Querying LDAP path {Resource}", resource);
        return await Task.Run(() =>
        {
            using var entry = _useProcessContext
                ? new DirectoryEntry($"LDAP://{resource}")
                : new DirectoryEntry($"LDAP://{resource}", _username, _password);
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
