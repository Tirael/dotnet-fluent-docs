using System.Globalization;
using System.Reflection;

namespace FluentDocs.Analysis;

internal static class ReflectionHelpers
{
    internal static object? GetPropertyValue(object instance, string name)
        => instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);

    internal static T? GetPropertyValue<T>(object instance, string name)
    {
        var value = GetPropertyValue(instance, name);
        return value is T typed ? typed : default;
    }

    internal static object? Invoke(object instance, string methodName, params object?[] args)
    {
        var type = instance.GetType();
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)
                     ?? type.GetInterfaces()
                         .SelectMany(i => i.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                         .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == args.Length);
        return method?.Invoke(instance, args);
    }

    internal static bool ImplementsInterface(object instance, string interfaceName)
        => instance.GetType().GetInterfaces().Any(i => i.Name == interfaceName || i.FullName == interfaceName);

    internal static Type? FindInterface(object instance, string interfaceName)
        => instance.GetType().GetInterfaces().FirstOrDefault(i =>
            i.Name == interfaceName
            || (i.IsGenericType && i.GetGenericTypeDefinition().Name == interfaceName)
            || i.FullName == interfaceName);

    internal static string GetNonGenericName(Type type)
    {
        var name = type.Name;
        var tick = name.IndexOf('`', StringComparison.Ordinal);
        return tick >= 0 ? name[..tick] : name;
    }

    internal static string FormatClrType(Type type)
    {
        if (type.IsArray)
            return $"{FormatClrType(type.GetElementType()!)}[]";

        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return $"{FormatClrType(underlying)}?";

        if (type.IsGenericType)
        {
            var def = GetNonGenericName(type.GetGenericTypeDefinition());
            var args = string.Join(", ", type.GetGenericArguments().Select(FormatClrType));
            return $"{def}<{args}>";
        }

        return type.FullName switch
        {
            "System.String" => "string",
            "System.Int32" => "int",
            "System.Int64" => "long",
            "System.Int16" => "short",
            "System.Boolean" => "bool",
            "System.Decimal" => "decimal",
            "System.Double" => "double",
            "System.Single" => "float",
            "System.Byte" => "byte",
            "System.Object" => "object",
            "System.Guid" => "Guid",
            "System.TimeSpan" => "TimeSpan",
            "System.DateTime" => "DateTime",
            "System.DateTimeOffset" => "DateTimeOffset",
            "System.Uri" => "Uri",
            _ => type.Name
        };
    }

    internal static string? FormatDefaultValue(object? value)
    {
        switch (value)
        {
            case null:
                return "null";
            case string s:
                return $"\"{s}\"";
            case bool b:
                return b ? "true" : "false";
            case IFormattable formattable:
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            case System.Collections.IEnumerable enumerable:
                {
                    var items = enumerable.Cast<object?>().Select(FormatDefaultValue).ToArray();
                    return items.Length == 0 ? "[]" : $"[{string.Join(", ", items)}]";
                }
            default:
                {
                    var type = value.GetType();
                    if (type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true)
                        return Convert.ToString(value, CultureInfo.InvariantCulture);

                    return null;
                }
        }
    }

    internal static bool IsSimpleType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(Guid)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(TimeSpan)
               || type == typeof(Uri);
    }

    internal static bool IsCollection(Type type)
    {
        if (type == typeof(string))
            return false;
        return typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
    }

    internal static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        var enumerable = type.GetInterfaces()
            .Concat([type])
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }

    internal static string DocumentationId(Type type)
    {
        var fullName = type.FullName ?? type.Name;
        return "T:" + fullName.Replace('+', '.');
    }

    internal static string DocumentationId(PropertyInfo property)
    {
        var declaring = property.DeclaringType ?? property.ReflectedType;
        var typeId = (declaring?.FullName ?? "").Replace('+', '.');
        return $"P:{typeId}.{property.Name}";
    }
}
