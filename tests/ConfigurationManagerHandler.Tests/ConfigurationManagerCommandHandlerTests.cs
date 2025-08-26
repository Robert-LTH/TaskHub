using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConfigurationManagerHandler;
using Microsoft.Extensions.Configuration;
using TaskHub.Abstractions;
using Xunit;

namespace ConfigurationManagerHandler.Tests;

public class ConfigurationManagerCommandHandlerTests
{
    [Fact]
    public void CommandsIncludeCmQuery()
    {
        var handler = new ConfigurationManagerCommandHandler();
        Assert.Contains("cm-query", handler.Commands);
    }

    [Fact]
    public void ServiceNameReflectsConfiguration()
    {
        var adminConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PluginSettings:ConfigurationManager:UseAdminService"] = "true"
        }).Build();
        var adminHandler = new ConfigurationManagerCommandHandler(adminConfig);
        Assert.Equal("configurationmanageradmin", adminHandler.ServiceName);

        var wmiConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PluginSettings:ConfigurationManager:UseAdminService"] = "false"
        }).Build();
        var wmiHandler = new ConfigurationManagerCommandHandler(wmiConfig);
        Assert.Equal("configurationmanager", wmiHandler.ServiceName);
    }

    [Fact]
    public async Task ExecuteUsesAdminServiceWhenConfigured()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PluginSettings:ConfigurationManager:UseAdminService"] = "true"
        }).Build();
        var handler = new ConfigurationManagerCommandHandler(config);
        var request = new QueryRequest
        {
            BaseUrl = "http://localhost",
            Resource = "test"
        };
        var payload = JsonSerializer.SerializeToElement(request);
        var plugin = new FakeAdminServicePlugin();
        await handler.ExecuteAsync(payload, plugin, CancellationToken.None);
        Assert.True(plugin.Service.Called);
        Assert.Equal("http://localhost", plugin.Service.BaseUrl);
        Assert.Equal("test", plugin.Service.Resource);
    }

    [Fact]
    public async Task ExecuteUsesWmiServiceWhenConfigured()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PluginSettings:ConfigurationManager:UseAdminService"] = "false"
        }).Build();
        var handler = new ConfigurationManagerCommandHandler(config);
        var request = new QueryRequest
        {
            Host = ".",
            Namespace = "root\\cimv2",
            Query = "SELECT * FROM Win32_ComputerSystem"
        };
        var payload = JsonSerializer.SerializeToElement(request);
        var plugin = new FakeWmiServicePlugin();
        var result = await handler.ExecuteAsync(payload, plugin, CancellationToken.None);
        Assert.True(plugin.Service.Called);
        Assert.Equal(".", plugin.Service.Host);
        Assert.Equal("root\\cimv2", plugin.Service.Namespace);
        Assert.Equal("SELECT * FROM Win32_ComputerSystem", plugin.Service.Query);
        Assert.Equal("success", result.Result);
    }

    private class FakeAdminServicePlugin : IServicePlugin
    {
        public string Name => "configurationmanageradmin";
        public FakeAdminService Service { get; } = new();
        public object GetService() => Service;

        public class FakeAdminService
        {
            public bool Called;
            public string? BaseUrl;
            public string? Resource;

            public Task<OperationResult> Get(string baseUrl, string resource, CancellationToken cancellationToken = default)
            {
                Called = true;
                BaseUrl = baseUrl;
                Resource = resource;
                return Task.FromResult(new OperationResult(null, "success"));
            }
        }
    }

    private class FakeWmiServicePlugin : IServicePlugin
    {
        public string Name => "configurationmanager";
        public FakeWmiService Service { get; } = new();
        public object GetService() => Service;

        public class FakeWmiService
        {
            public bool Called;
            public string? Host;
            public string? Namespace;
            public string? Query;

            public OperationResult Query(string host, string wmiNamespace, string query)
            {
                Called = true;
                Host = host;
                Namespace = wmiNamespace;
                Query = query;
                return new OperationResult(null, "success");
            }
        }
    }
}
