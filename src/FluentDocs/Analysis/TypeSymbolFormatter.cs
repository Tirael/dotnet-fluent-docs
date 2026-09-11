using Microsoft.CodeAnalysis;

namespace FluentDocs.Analysis;

/// <summary>
/// Короткое отображаемое имя CLR-типа для каталога.
/// </summary>
internal static class TypeSymbolFormatter
{
    public static string Format(ITypeSymbol? type)
    {
        if (type is null)
            return "object";

        if (type is IArrayTypeSymbol array)
            return $"{Format(array.ElementType)}[]";

        if (type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && named.TypeArguments.Length == 1)
        {
            return $"{Format(named.TypeArguments[0])}?";
        }

        if (type is INamedTypeSymbol generic && generic.IsGenericType)
        {
            var name = generic.Name;
            var args = string.Join(", ", generic.TypeArguments.Select(Format));
            return $"{name}<{args}>";
        }

        return type.SpecialType switch
        {
            SpecialType.System_String => "string",
            SpecialType.System_Int32 => "int",
            SpecialType.System_Int64 => "long",
            SpecialType.System_Int16 => "short",
            SpecialType.System_Boolean => "bool",
            SpecialType.System_Decimal => "decimal",
            SpecialType.System_Double => "double",
            SpecialType.System_Single => "float",
            SpecialType.System_Byte => "byte",
            SpecialType.System_SByte => "sbyte",
            SpecialType.System_UInt16 => "ushort",
            SpecialType.System_UInt32 => "uint",
            SpecialType.System_UInt64 => "ulong",
            SpecialType.System_Object => "object",
            SpecialType.System_Char => "char",
            _ => type.Name
        };
    }

    public static bool IsCollection(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
            return false;
        return GetCollectionElementType(type) is not null;
    }

    public static ITypeSymbol? GetCollectionElementType(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
            return null;

        if (type is IArrayTypeSymbol array)
            return array.ElementType;

        foreach (var candidate in type.AllInterfaces.Concat([type]))
        {
            if (candidate is INamedTypeSymbol { Name: "IEnumerable", Arity: 1 } enumerable)
                return enumerable.TypeArguments[0];
        }

        return null;
    }

    public static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && named.TypeArguments.Length == 1)
        {
            return named.TypeArguments[0];
        }

        return type;
    }
}
