using System;
using System.Collections.Generic;
using System.Text.Json;
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

        public async Task<OperationResult> QueryAsync(string query)
        {
            try
            {
                await using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                await using var command = new SqlCommand(query, connection);
                await using var reader = await command.ExecuteReaderAsync();
                var rows = new List<Dictionary<string, object?>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[reader.GetName(i)] = value;
                    }
                    rows.Add(row);
                }
                var element = JsonSerializer.SerializeToElement(rows);
                return new OperationResult(element, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to execute query: {ex.Message}");
            }
        }

        public Task<OperationResult> InsertAsync(string commandText) => ExecuteNonQueryAsync(commandText);

        public Task<OperationResult> UpdateAsync(string commandText) => ExecuteNonQueryAsync(commandText);

        public Task<OperationResult> DeleteAsync(string commandText) => ExecuteNonQueryAsync(commandText);

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
    }
}

