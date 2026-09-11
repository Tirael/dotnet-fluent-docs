using System.Globalization;

namespace FluentDocs.Analysis;

/// <summary>
/// Форматирует константы и значения по умолчанию так же, как раньше делала рефлексия.
/// </summary>
internal static class ValueFormatter
{
    public static string FormatConstant(object? value)
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
            default:
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
        }
    }

    /// <summary>
    /// Значение для идентификатора правила: строки без кавычек.
    /// </summary>
    public static string FormatCompare(object? value)
        => FormatConstant(value).Trim('"');

    public static string? FormatTypeDefault(Microsoft.CodeAnalysis.ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            Microsoft.CodeAnalysis.SpecialType.System_Boolean => "false",
            Microsoft.CodeAnalysis.SpecialType.System_Char => "'\\0'",
            Microsoft.CodeAnalysis.SpecialType.System_SByte
                or Microsoft.CodeAnalysis.SpecialType.System_Byte
                or Microsoft.CodeAnalysis.SpecialType.System_Int16
                or Microsoft.CodeAnalysis.SpecialType.System_UInt16
                or Microsoft.CodeAnalysis.SpecialType.System_Int32
                or Microsoft.CodeAnalysis.SpecialType.System_UInt32
                or Microsoft.CodeAnalysis.SpecialType.System_Int64
                or Microsoft.CodeAnalysis.SpecialType.System_UInt64
                or Microsoft.CodeAnalysis.SpecialType.System_Decimal
                or Microsoft.CodeAnalysis.SpecialType.System_Single
                or Microsoft.CodeAnalysis.SpecialType.System_Double => "0",
            _ when type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum => "0",
            _ => null
        };
    }
}
