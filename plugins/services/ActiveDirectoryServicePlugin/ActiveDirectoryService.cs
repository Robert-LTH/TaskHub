using System;
using System.DirectoryServices;
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

    public string Name => "activedirectory";

    public object GetService() => (Func<string, DirectorySearcher>)(path =>
    {
        _logger.LogInformation("Creating searcher for {Path}", path);
        var entry = _useProcessContext
            ? new DirectoryEntry($"LDAP://{path}")
            : new DirectoryEntry($"LDAP://{path}", _username, _password);
        return new DirectorySearcher(entry);
    });
}

