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
    public void CommandsIncludeExpectedValues()
    {
        var handler = new ConfigurationManagerCommandHandler();
        Assert.Contains("cm-query", handler.Commands);
        Assert.Contains("cm-invoke", handler.Commands);
        Assert.Contains("cm-errorcode", handler.Commands);
        Assert.Contains("cm-adddevice", handler.Commands);
        Assert.Contains("cm-adduser", handler.Commands);
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
            Resource = "test",
        };
        var payload = JsonSerializer.SerializeToElement(request);
        var plugin = new FakeAdminServicePlugin();
        await handler.ExecuteAsync(payload, plugin, CancellationToken.None);
        Assert.True(plugin.Service.Called);
        Assert.Equal("http://localhost", plugin.Service.BaseUrl);
        Assert.Equal("test", plugin.Service.Resource);
    }

    [Fact]
    public async Task ExecuteQueryUsesWmiServiceWhenConfigured()
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
            Query = "SELECT * FROM Win32_ComputerSystem",
        };
        var payload = JsonSerializer.SerializeToElement(request);
        var plugin = new FakeWmiServicePlugin();
        var result = await handler.ExecuteAsync(payload, plugin, CancellationToken.None);
        Assert.True(plugin.Service.QueryCalled);
        Assert.Equal(".", plugin.Service.Host);
        Assert.Equal("root\\cimv2", plugin.Service.Namespace);
        Assert.Equal("SELECT * FROM Win32_ComputerSystem", plugin.Service.QueryString);
        Assert.Equal("success", result.Result);
    }

    [Fact]
    public async Task ExecuteInvokeMethod()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PluginSettings:ConfigurationManager:UseAdminService"] = "false"
        }).Build();
        var handler = new ConfigurationManagerCommandHandler(config);
        var request = new InvokeMethodRequest
        {
            Host = ".",
            Namespace = "root\\cimv2",
            Path = "Win32_Process",
            Method = "Create",
            Parameters = new Dictionary<string, object?> { ["CommandLine"] = "cmd.exe" }
        };
        var payload = JsonSerializer.SerializeToElement(request);
        var plugin = new FakeWmiServicePlugin();
        var result = await handler.ExecuteAsync(payload, plugin, CancellationToken.None);
        Assert.True(plugin.Service.InvokeCalled);
        Assert.Equal("Win32_Process", plugin.Service.Path);
        Assert.Equal("Create", plugin.Service.Method);
        Assert.Equal("success", result.Result);
    }

    [Fact]
    public async Task ExecuteGetErrorCode()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PluginSettings:ConfigurationManager:UseAdminService"] = "false"
        }).Build();
        var handler = new ConfigurationManagerCommandHandler(config);
        var request = new GetErrorCodeRequest
        {
            Host = ".",
            Namespace = "root\\cimv2",
            Class = "Win32_PnPEntity",
            PnpDeviceId = "DEVICE"
        };
        var payload = JsonSerializer.SerializeToElement(request);
        var plugin = new FakeWmiServicePlugin();
        var result = await handler.ExecuteAsync(payload, plugin, CancellationToken.None);
        Assert.True(plugin.Service.ErrorCalled);
        Assert.Equal("DEVICE", plugin.Service.PnpDeviceId);
        Assert.Equal("success", result.Result);
    }

    private static readonly string[] expectedDeviceIds = new[] { "DEV1", "DEV2" };

    [Fact]
    public async Task ExecuteAddDeviceToCollection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PluginSettings:ConfigurationManager:UseAdminService"] = "false"
        }).Build();
        var handler = new ConfigurationManagerCommandHandler(config);
        var request = new AddDeviceToCollectionRequest
        {
            Host = ".",
            Namespace = "root\\cimv2",
            CollectionId = "COLL",
            DeviceIds = new[] { "DEV1", "DEV2" }
        };
        var payload = JsonSerializer.SerializeToElement(request);
        var plugin = new FakeWmiServicePlugin();
        var result = await handler.ExecuteAsync(payload, plugin, CancellationToken.None);
        Assert.True(plugin.Service.AddDeviceCalled);
        Assert.Equal("COLL", plugin.Service.CollectionId);
        Assert.Equal(expectedDeviceIds, plugin.Service.DeviceIds);
        Assert.Equal("success", result.Result);
    }

    private static readonly string[] expectedUserIds = new[] { "USER1", "USER2" };

    [Fact]
    public async Task ExecuteAddUserToCollection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PluginSettings:ConfigurationManager:UseAdminService"] = "false"
        }).Build();
        var handler = new ConfigurationManagerCommandHandler(config);
        var request = new AddUserToCollectionRequest
        {
            Host = ".",
            Namespace = "root\\cimv2",
            CollectionId = "UCOLL",
            UserIds = new[] { "USER1", "USER2" }
        };
        var payload = JsonSerializer.SerializeToElement(request);
        var plugin = new FakeWmiServicePlugin();
        var result = await handler.ExecuteAsync(payload, plugin, CancellationToken.None);
        Assert.True(plugin.Service.AddUserCalled);
        Assert.Equal("UCOLL", plugin.Service.CollectionId);
        Assert.Equal(expectedUserIds, plugin.Service.UserIds);
        Assert.Equal("success", result.Result);
    }

    // reuse expectedDeviceIds

    [Fact]
    public async Task ExecuteAddDeviceToCollectionUsesAdminService()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PluginSettings:ConfigurationManager:UseAdminService"] = "true"
        }).Build();
        var handler = new ConfigurationManagerCommandHandler(config);
        var request = new AddDeviceToCollectionRequest
        {
            BaseUrl = "http://localhost",
            CollectionId = "COLL",
            DeviceIds = new[] { "DEV1", "DEV2" }
        };
        var payload = JsonSerializer.SerializeToElement(request);
        var plugin = new FakeAdminServicePlugin();
        await handler.ExecuteAsync(payload, plugin, CancellationToken.None);
        Assert.True(plugin.Service.AddDeviceCalled);
        Assert.Equal("http://localhost", plugin.Service.BaseUrl);
        Assert.Equal("COLL", plugin.Service.CollectionId);
        Assert.Equal(expectedDeviceIds, plugin.Service.DeviceIds);
    }

    // reuse expectedUserIds

    [Fact]
    public async Task ExecuteAddUserToCollectionUsesAdminService()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PluginSettings:ConfigurationManager:UseAdminService"] = "true"
        }).Build();
        var handler = new ConfigurationManagerCommandHandler(config);
        var request = new AddUserToCollectionRequest
        {
            BaseUrl = "http://localhost",
            CollectionId = "UCOLL",
            UserIds = new[] { "USER1", "USER2" }
        };
        var payload = JsonSerializer.SerializeToElement(request);
        var plugin = new FakeAdminServicePlugin();
        await handler.ExecuteAsync(payload, plugin, CancellationToken.None);
        Assert.True(plugin.Service.AddUserCalled);
        Assert.Equal("http://localhost", plugin.Service.BaseUrl);
        Assert.Equal("UCOLL", plugin.Service.CollectionId);
        Assert.Equal(expectedUserIds, plugin.Service.UserIds);
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
            public bool AddDeviceCalled;
            public bool AddUserCalled;
            public string? CollectionId;
            public string[]? DeviceIds;
            public string[]? UserIds;

            public Task<OperationResult> Get(string baseUrl, string resource, CancellationToken cancellationToken = default)
            {
                Called = true;
                BaseUrl = baseUrl;
                Resource = resource;
                return Task.FromResult(new OperationResult(null, "success"));
            }

            public Task<OperationResult> AddDeviceToCollection(string baseUrl, string collectionId, string[] deviceIds, CancellationToken cancellationToken = default)
            {
                AddDeviceCalled = true;
                BaseUrl = baseUrl;
                CollectionId = collectionId;
                DeviceIds = deviceIds;
                return Task.FromResult(new OperationResult(null, "success"));
            }

            public Task<OperationResult> AddUserToCollection(string baseUrl, string collectionId, string[] userIds, CancellationToken cancellationToken = default)
            {
                AddUserCalled = true;
                BaseUrl = baseUrl;
                CollectionId = collectionId;
                UserIds = userIds;
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
            public bool QueryCalled;
            public bool InvokeCalled;
            public bool ErrorCalled;
            public bool AddDeviceCalled;
            public bool AddUserCalled;
            public string? Host;
            public string? Namespace;
            public string? QueryString;
            public string? Path;
            public string? Method;
            public Dictionary<string, object?>? Parameters;
            public string? Class;
            public string? PnpDeviceId;
            public string? CollectionId;
            public string[]? DeviceIds;
            public string[]? UserIds;

            public OperationResult Query(string host, string wmiNamespace, string query)
            {
                QueryCalled = true;
                Host = host;
                Namespace = wmiNamespace;
                QueryString = query;
                return new OperationResult(null, "success");
            }

            public OperationResult InvokeMethod(string host, string wmiNamespace, string path, string method, Dictionary<string, object?>? parameters)
            {
                InvokeCalled = true;
                Host = host;
                Namespace = wmiNamespace;
                Path = path;
                Method = method;
                Parameters = parameters;
                return new OperationResult(null, "success");
            }

            public OperationResult GetErrorCode(string host, string wmiNamespace, string @class, string pnpDeviceId)
            {
                ErrorCalled = true;
                Host = host;
                Namespace = wmiNamespace;
                Class = @class;
                PnpDeviceId = pnpDeviceId;
                return new OperationResult(null, "success");
            }

            public OperationResult AddDeviceToCollection(string host, string wmiNamespace, string collectionId, string[] deviceIds)
            {
                AddDeviceCalled = true;
                Host = host;
                Namespace = wmiNamespace;
                CollectionId = collectionId;
                DeviceIds = deviceIds;
                return new OperationResult(null, "success");
            }

            public OperationResult AddUserToCollection(string host, string wmiNamespace, string collectionId, string[] userIds)
            {
                AddUserCalled = true;
                Host = host;
                Namespace = wmiNamespace;
                CollectionId = collectionId;
                UserIds = userIds;
                return new OperationResult(null, "success");
            }
        }
    }
}
