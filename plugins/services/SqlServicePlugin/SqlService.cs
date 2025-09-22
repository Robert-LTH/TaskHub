using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using TaskHub.Abstractions;

namespace SqlServicePlugin;

public class SqlServicePlugin : IServicePlugin
{
    public string Name => "sql";

    public object GetService() => (Func<string, SqlService>)(connectionString => new SqlService(connectionString));

    public class SqlService
    {
        private readonly string _connectionString;

        public SqlService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<OperationResult> QueryAsync(string query, CancellationToken cancellationToken = default)
        {
            try
            {
                var rows = await QueryInternalAsync(query, static record => ReadRow(record), cancellationToken);
                var element = JsonSerializer.SerializeToElement(rows);
                return new OperationResult(element, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to execute query: {ex.Message}");
            }
        }

        public Task<IReadOnlyList<T>> QueryAsync<T>(string query, CancellationToken cancellationToken = default)
            where T : new()
        {
            return QueryInternalAsync(query, static record => SqlRowMapper.MapToType<T>(record), cancellationToken);
        }

        public Task<OperationResult> InsertAsync(string commandText) => ExecuteNonQueryAsync(commandText);

        public Task<OperationResult> UpdateAsync(string commandText) => ExecuteNonQueryAsync(commandText);

        public Task<OperationResult> DeleteAsync(string commandText) => ExecuteNonQueryAsync(commandText);

        private async Task<IReadOnlyList<T>> QueryInternalAsync<T>(string query, Func<IDataRecord, T> map, CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var results = new List<T>();
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(map(reader));
            }

            return results;
        }

        private async Task<OperationResult> ExecuteNonQueryAsync(string commandText)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                await using var command = new SqlCommand(commandText, connection);
                var affected = await command.ExecuteNonQueryAsync();
                var element = JsonSerializer.SerializeToElement(affected);
                return new OperationResult(element, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to execute command: {ex.Message}");
            }
        }

        private static Dictionary<string, object?> ReadRow(IDataRecord record)
        {
            var row = new Dictionary<string, object?>(record.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < record.FieldCount; i++)
            {
                var value = record.IsDBNull(i) ? null : record.GetValue(i);
                row[record.GetName(i)] = value;
            }

            return row;
        }
    }
}

