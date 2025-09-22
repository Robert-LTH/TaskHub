using System;
using System.Data;
using System.Globalization;

namespace SqlServicePlugin.Tests;

internal sealed class FakeDataRecord : IDataRecord
{
    private readonly string[] _names;
    private readonly object?[] _values;

    public FakeDataRecord(params (string Name, object? Value)[] values)
    {
        _names = new string[values.Length];
        _values = new object?[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            _names[i] = values[i].Name;
            _values[i] = values[i].Value;
        }
    }

    public int FieldCount => _values.Length;

    public object this[int i] => _values[i]!;

    public object this[string name] => _values[GetOrdinal(name)]!;

    public string GetName(int i) => _names[i];

    public string GetDataTypeName(int i) => GetFieldType(i).Name;

    public Type GetFieldType(int i)
    {
        var value = _values[i];
        return value?.GetType() ?? typeof(DBNull);
    }

    public object GetValue(int i) => _values[i]!;

    public int GetValues(object[] values)
    {
        var length = Math.Min(values.Length, _values.Length);
        Array.Copy(_values, values, length);
        return length;
    }

    public int GetOrdinal(string name)
    {
        for (var i = 0; i < _names.Length; i++)
        {
            if (string.Equals(_names[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new IndexOutOfRangeException($"Column '{name}' not found.");
    }

    public bool GetBoolean(int i) => Convert.ToBoolean(_values[i], CultureInfo.InvariantCulture);

    public byte GetByte(int i) => Convert.ToByte(_values[i], CultureInfo.InvariantCulture);

    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();

    public char GetChar(int i) => Convert.ToChar(_values[i], CultureInfo.InvariantCulture);

    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();

    public Guid GetGuid(int i) => _values[i] switch
    {
        Guid guid => guid,
        string str => Guid.Parse(str),
        _ => Guid.Parse(Convert.ToString(_values[i], CultureInfo.InvariantCulture) ?? string.Empty)
    };

    public short GetInt16(int i) => Convert.ToInt16(_values[i], CultureInfo.InvariantCulture);

    public int GetInt32(int i) => Convert.ToInt32(_values[i], CultureInfo.InvariantCulture);

    public long GetInt64(int i) => Convert.ToInt64(_values[i], CultureInfo.InvariantCulture);

    public float GetFloat(int i) => Convert.ToSingle(_values[i], CultureInfo.InvariantCulture);

    public double GetDouble(int i) => Convert.ToDouble(_values[i], CultureInfo.InvariantCulture);

    public string GetString(int i) => Convert.ToString(_values[i], CultureInfo.InvariantCulture) ?? string.Empty;

    public decimal GetDecimal(int i) => Convert.ToDecimal(_values[i], CultureInfo.InvariantCulture);

    public DateTime GetDateTime(int i) => Convert.ToDateTime(_values[i], CultureInfo.InvariantCulture);

    public IDataReader GetData(int i) => throw new NotSupportedException();

    public bool IsDBNull(int i) => _values[i] is null || _values[i] is DBNull;
}
