using System;
using Xunit;

namespace SqlServicePlugin.Tests;

public class SqlRowMapperTests
{
    private enum Status
    {
        Unknown = 0,
        Active = 1
    }

    private sealed class SampleRecord
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public DateTime Created { get; set; }
    }

    [Fact]
    public void MapToType_PopulatesWritableProperties()
    {
        var record = new FakeDataRecord(
            ("Id", 42),
            ("Name", "TaskHub"),
            ("Created", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        );

        var result = SqlRowMapper.MapToType<SampleRecord>(record);

        Assert.Equal(42, result.Id);
        Assert.Equal("TaskHub", result.Name);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.Created);
    }

    private sealed class NullableRecord
    {
        public int? Count { get; set; }

        public string? Missing { get; set; }
    }

    [Fact]
    public void MapToType_HandlesNullableProperties()
    {
        var record = new FakeDataRecord(
            ("Count", DBNull.Value),
            ("Ignored", "value")
        );

        var result = SqlRowMapper.MapToType<NullableRecord>(record);

        Assert.Null(result.Count);
        Assert.Null(result.Missing);
    }

    private sealed class ConversionRecord
    {
        public Status Status { get; set; }

        public Guid Identifier { get; set; }

        public TimeSpan Duration { get; set; }
    }

    [Fact]
    public void MapToType_PerformsCommonConversions()
    {
        var identifier = Guid.NewGuid();
        var record = new FakeDataRecord(
            ("Status", "Active"),
            ("Identifier", identifier.ToString()),
            ("Duration", "00:00:05")
        );

        var result = SqlRowMapper.MapToType<ConversionRecord>(record);

        Assert.Equal(Status.Active, result.Status);
        Assert.Equal(identifier, result.Identifier);
        Assert.Equal(TimeSpan.FromSeconds(5), result.Duration);
    }

    private sealed class NonNullableRecord
    {
        public int Count { get; set; }
    }

    [Fact]
    public void MapToType_ThrowsWhenConversionFails()
    {
        var record = new FakeDataRecord(("Count", "invalid"));

        Assert.Throws<InvalidOperationException>(() => SqlRowMapper.MapToType<NonNullableRecord>(record));
    }
}
