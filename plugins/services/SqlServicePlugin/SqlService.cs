using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using TaskHub.Abstractions;

namespace SqlServicePlugin;

public class SqlServicePlugin : IServicePlugin
{
    private readonly IConfiguration _configuration;
    private SqlServiceRegistry? _registry;

    public SqlServicePlugin(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IServiceProvider Services { get; private set; } = default!;

    public string Name => "sql";

    public void OnLoaded(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public object GetService()
    {
        EnsureConnectionsInitialized();
        return _registry ?? throw new InvalidOperationException("SQL registry not initialized.");
    }

    private void EnsureConnectionsInitialized()
    {
        if (_registry != null)
        {
            return;
        }

        var connectionsSection = _configuration.GetSection("PluginSettings:Sql:Connections");
        if (!connectionsSection.Exists())
        {
            throw new InvalidOperationException("No SQL connections configured under PluginSettings:Sql:Connections.");
        }

        var connections = connectionsSection.GetChildren()
            .Where(section => !string.IsNullOrWhiteSpace(section.Value))
            .ToDictionary(section => section.Key, section => section.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        if (connections.Count == 0)
        {
            throw new InvalidOperationException("No valid SQL connection strings were found in configuration.");
        }

        var services = connections.ToDictionary(
            kvp => kvp.Key,
            kvp => new SqlService(kvp.Value),
            StringComparer.OrdinalIgnoreCase);

        _registry = new SqlServiceRegistry(services);
    }

    public sealed class SqlServiceRegistry
    {
        private readonly IReadOnlyDictionary<string, SqlService> _services;

        public SqlServiceRegistry(IReadOnlyDictionary<string, SqlService> services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public SqlService this[string name]
        {
            get
            {
                if (!_services.TryGetValue(name, out var service))
                {
                    throw new KeyNotFoundException($"SQL connection '{name}' is not configured.");
                }

                return service;
            }
        }

        public bool TryGet(string name, out SqlService service) => _services.TryGetValue(name, out service!);
        public IEnumerable<string> Names => _services.Keys;
    }

    public class SqlService
    {
        private static readonly Regex IdentifierPattern = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly string _connectionString;

        public SqlService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
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

        public async Task<OperationResult> BulkUpdateAsync(
            string tableName,
            string keyColumn,
            IEnumerable<IDictionary<string, object?>> rows,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(keyColumn)) throw new ArgumentNullException(nameof(keyColumn));
            if (rows is null) throw new ArgumentNullException(nameof(rows));

            var sanitizedTable = EscapeIdentifier(tableName);
            var sanitizedKey = EscapeIdentifier(keyColumn);

            var rowList = rows.ToList();
            if (rowList.Count == 0)
            {
                return new OperationResult(JsonSerializer.SerializeToElement(0), "success");
            }

            var preparedRows = new List<(IDictionary<string, object?> Data, object? KeyValue)>(rowList.Count);
            var columnOrder = new List<string>();
            var sanitizedColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var updateFlagColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rowList)
            {
                if (!TryGetValueCaseInsensitive(row, keyColumn, out var keyValue))
                {
                    throw new InvalidOperationException($"Row is missing key column '{keyColumn}'.");
                }

                preparedRows.Add((row, keyValue));

                foreach (var columnName in row.Keys)
                {
                    if (string.Equals(columnName, keyColumn, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!sanitizedColumns.ContainsKey(columnName))
                    {
                        sanitizedColumns[columnName] = EscapeIdentifier(columnName);
                        columnOrder.Add(columnName);
                        updateFlagColumns[columnName] = EscapeIdentifier($"__th_update_{columnName}");
                    }
                }
            }

            if (columnOrder.Count == 0)
            {
                var payload = JsonSerializer.SerializeToElement(new { RowsAffected = 0, RowsProcessed = rowList.Count });
                return new OperationResult(payload, "success");
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            var tempTableName = $"#Bulk_{Guid.NewGuid():N}";
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await using (var createCommand = connection.CreateCommand())
                {
                    createCommand.Transaction = transaction;
                    createCommand.CommandText = $"SELECT TOP 0 * INTO {tempTableName} FROM {sanitizedTable};";
                    await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var columnName in columnOrder)
                {
                    await using var alterCommand = connection.CreateCommand();
                    alterCommand.Transaction = transaction;
                    alterCommand.CommandText = $"ALTER TABLE {tempTableName} ADD {updateFlagColumns[columnName]} BIT NOT NULL DEFAULT 0;";
                    await alterCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                var insertColumnList = new List<string> { sanitizedKey };
                insertColumnList.AddRange(columnOrder.Select(col => sanitizedColumns[col]));
                insertColumnList.AddRange(columnOrder.Select(col => updateFlagColumns[col]));
                var insertColumnsClause = string.Join(", ", insertColumnList);

                foreach (var entry in preparedRows)
                {
                    await using var insertCommand = connection.CreateCommand();
                    insertCommand.Transaction = transaction;

                    var parameterNames = new List<string>();
                    var keyParameter = insertCommand.CreateParameter();
                    keyParameter.ParameterName = "@p0";
                    keyParameter.Value = entry.KeyValue ?? DBNull.Value;
                    insertCommand.Parameters.Add(keyParameter);
                    parameterNames.Add(keyParameter.ParameterName);

                    var hasColumnFlags = new List<bool>(columnOrder.Count);
                    var paramIndex = 1;
                    foreach (var columnName in columnOrder)
                    {
                        var parameterName = $"@p{paramIndex++}";
                        var hasValue = TryGetValueCaseInsensitive(entry.Data, columnName, out var value);
                        insertCommand.Parameters.AddWithValue(parameterName, hasValue ? value ?? DBNull.Value : DBNull.Value);
                        parameterNames.Add(parameterName);
                        hasColumnFlags.Add(hasValue);
                    }

                    for (var i = 0; i < columnOrder.Count; i++)
                    {
                        var flagParameterName = $"@p{paramIndex++}";
                        insertCommand.Parameters.AddWithValue(flagParameterName, hasColumnFlags[i] ? 1 : 0);
                        parameterNames.Add(flagParameterName);
                    }

                    insertCommand.CommandText = $"INSERT INTO {tempTableName} ({insertColumnsClause}) VALUES ({string.Join(", ", parameterNames)});";
                    await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                var updateAssignments = columnOrder.Select(columnName =>
                    $"Target.{sanitizedColumns[columnName]} = CASE WHEN Source.{updateFlagColumns[columnName]} = 1 THEN Source.{sanitizedColumns[columnName]} ELSE Target.{sanitizedColumns[columnName]} END");
                var setClause = string.Join(", ", updateAssignments);

                await using (var mergeCommand = connection.CreateCommand())
                {
                    mergeCommand.Transaction = transaction;
                    mergeCommand.CommandText = $@"MERGE {sanitizedTable} AS Target
USING {tempTableName} AS Source
    ON Target.{sanitizedKey} = Source.{sanitizedKey}
WHEN MATCHED THEN
    UPDATE SET {setClause};";

                    var affected = await mergeCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                    var resultPayload = JsonSerializer.SerializeToElement(new { RowsAffected = affected, RowsProcessed = rowList.Count });
                    return new OperationResult(resultPayload, "success");
                }
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }


        public async Task<OperationResult> UpsertAsync(
            string tableName,
            string keyColumn,
            IEnumerable<IDictionary<string, object?>> rows,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(keyColumn)) throw new ArgumentNullException(nameof(keyColumn));
            if (rows is null) throw new ArgumentNullException(nameof(rows));

            var sanitizedTable = EscapeIdentifier(tableName);
            var sanitizedKey = EscapeIdentifier(keyColumn);

            var rowList = rows.ToList();
            if (rowList.Count == 0)
            {
                return new OperationResult(JsonSerializer.SerializeToElement(0), "success");
            }

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var affected = 0;

                foreach (var row in rowList)
                {
                    if (!row.TryGetValue(keyColumn, out var keyValue))
                    {
                        throw new InvalidOperationException($"Row is missing key column '{keyColumn}'.");
                    }

                    var columns = row.Keys
                        .Where(k => !string.Equals(k, keyColumn, StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (columns.Length == 0)
                    {
                        continue; // nothing to update/insert besides key
                    }

                    var sanitizedColumns = columns.Select(EscapeIdentifier).ToArray();

                    var insertColumnList = string.Join(", ", sanitizedColumns.Prepend(sanitizedKey));
                    var insertValuesList = string.Join(", ", columns.Prepend(keyColumn).Select((col, index) => $"@p{index}"));
                    var updateSetClause = string.Join(", ", columns.Select((col, index) => $"{sanitizedColumns[index]} = @u{index}"));

                    var commandText = $@"
IF EXISTS (SELECT 1 FROM {sanitizedTable} WHERE {sanitizedKey} = @key)
    UPDATE {sanitizedTable}
        SET {updateSetClause}
        WHERE {sanitizedKey} = @key;
ELSE
    INSERT INTO {sanitizedTable} ({insertColumnList}) VALUES ({insertValuesList});";

                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = commandText;

                    command.Parameters.AddWithValue("@key", keyValue ?? DBNull.Value);

                    for (var i = 0; i < columns.Length; i++)
                    {
                        var value = row.TryGetValue(columns[i], out var val) ? val : null;
                        command.Parameters.AddWithValue($"@u{i}", value ?? DBNull.Value);
                        command.Parameters.AddWithValue($"@p{i + 1}", value ?? DBNull.Value);
                    }

                    command.Parameters.AddWithValue("@p0", keyValue ?? DBNull.Value);

                    affected += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                var resultPayload = JsonSerializer.SerializeToElement(new { RowsAffected = affected, RowsProcessed = rowList.Count });
                return new OperationResult(resultPayload, "success");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        private static string EscapeIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || !IdentifierPattern.IsMatch(name))
            {
                throw new ArgumentException($"Identifier '{name}' is invalid. Only letters, numbers, and underscore are allowed, and it must not start with a number.", nameof(name));
            }

            return $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]";
        }

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

        private static bool TryGetValueCaseInsensitive(IDictionary<string, object?> source, string key, out object? value)
        {
            if (source.TryGetValue(key, out value))
            {
                return true;
            }

            foreach (var kvp in source)
            {
                if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = null;
            return false;
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
