using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using SqlServicePluginClass = SqlServicePlugin.SqlServicePlugin;
using Xunit;

namespace SqlServicePlugin.Tests;

public class SqlServiceTests
{
    private static SqlServicePluginClass.SqlService CreateService()
        => new("Server=localhost;Database=TaskHub;Integrated Security=True");

    [Fact]
    public async Task BulkUpdateAsync_ReturnsSuccess_WhenRowsEmpty()
    {
        var service = CreateService();

        var result = await service.BulkUpdateAsync("Users", "Id", Array.Empty<IDictionary<string, object?>>());

        Assert.Equal("success", result.Result);
        Assert.True(result.Payload.HasValue);
        Assert.Equal(0, result.Payload.Value.GetInt32());
    }

    [Fact]
    public async Task BulkUpdateAsync_Throws_WhenKeyMissing()
    {
        var service = CreateService();
        var rows = new[]
        {
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = "Alice"
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BulkUpdateAsync("Users", "Id", rows));
    }

    [Fact]
    public async Task BulkUpdateAsync_Throws_WhenColumnNameInvalid()
    {
        var service = CreateService();
        var rows = new[]
        {
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = 1,
                ["bad-name"] = "value"
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.BulkUpdateAsync("Users", "Id", rows));
    }

    [Fact]
    public async Task BulkUpdateAsync_ShortCircuits_WhenNoColumnsToUpdate()
    {
        var service = CreateService();
        var rows = new[]
        {
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = 1
            }
        };

        var result = await service.BulkUpdateAsync("Users", "Id", rows);

        Assert.Equal("success", result.Result);
        Assert.True(result.Payload.HasValue);
        var payload = result.Payload.Value;
        Assert.Equal(0, payload.GetProperty("RowsAffected").GetInt32());
        Assert.Equal(1, payload.GetProperty("RowsProcessed").GetInt32());
    }
}
