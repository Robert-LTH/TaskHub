using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Reflection;

namespace SqlServicePlugin;

internal static class SqlRowMapper
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropertyMaps = new();

    public static T MapToType<T>(IDataRecord record)
        where T : new()
    {
        var instance = new T();
        var propertyMap = PropertyMaps.GetOrAdd(typeof(T), CreatePropertyMap);

        for (var i = 0; i < record.FieldCount; i++)
        {
            if (!propertyMap.TryGetValue(record.GetName(i), out var property))
            {
                continue;
            }

            var rawValue = record.IsDBNull(i) ? null : record.GetValue(i);
            if (rawValue is null)
            {
                if (IsNullable(property.PropertyType))
                {
                    property.SetValue(instance, null);
                }

                continue;
            }

            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            try
            {
                var convertedValue = ConvertValue(rawValue, targetType);
                property.SetValue(instance, convertedValue);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to convert column '{record.GetName(i)}' to property '{property.Name}' of type '{property.PropertyType.FullName}'.",
                    ex);
            }
        }

        return instance;
    }

    private static Dictionary<string, PropertyInfo> CreatePropertyMap(Type type)
    {
        var map = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanWrite)
            {
                continue;
            }

            map[property.Name] = property;
        }

        return map;
    }

    private static bool IsNullable(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private static object ConvertValue(object value, Type targetType)
    {
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType.IsEnum)
        {
            if (value is string stringValue)
            {
                return Enum.Parse(targetType, stringValue, ignoreCase: true);
            }

            var underlying = Enum.GetUnderlyingType(targetType);
            var converted = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
            return Enum.ToObject(targetType, converted!);
        }

        if (targetType == typeof(Guid))
        {
            return value switch
            {
                Guid guidValue => guidValue,
                string guidString => Guid.Parse(guidString),
                _ => Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)
            };
        }

        if (targetType == typeof(DateTimeOffset))
        {
            return value switch
            {
                DateTimeOffset dto => dto,
                DateTime dt => new DateTimeOffset(dt),
                string str => DateTimeOffset.Parse(str, CultureInfo.InvariantCulture),
                _ => new DateTimeOffset(Convert.ToDateTime(value, CultureInfo.InvariantCulture))
            };
        }

        if (targetType == typeof(TimeSpan))
        {
            return value switch
            {
                TimeSpan ts => ts,
                string str => TimeSpan.Parse(str, CultureInfo.InvariantCulture),
                _ => TimeSpan.FromTicks(Convert.ToInt64(value, CultureInfo.InvariantCulture))
            };
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture)!;
    }
}
