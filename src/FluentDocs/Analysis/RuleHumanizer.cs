using System.Text;

namespace FluentDocs.Analysis;

/// <summary>
/// Превращает вызов FluentValidation в стабильный идентификатор и русское описание.
/// </summary>
internal static class RuleHumanizer
{
    public static SettingsRuleDocument? Humanize(
        string methodName,
        IReadOnlyList<string> arguments,
        string? message,
        bool hasCondition,
        string? ruleSet)
    {
        if (IsIgnored(methodName))
            return null;

        var (id, description) = Describe(methodName, arguments);
        var normalizedRuleSet = string.IsNullOrWhiteSpace(ruleSet) || ruleSet == "default" ? null : ruleSet;

        return new SettingsRuleDocument
        {
            Id = id,
            Description = description,
            Message = string.IsNullOrWhiteSpace(message) ? null : message,
            RuleSet = normalizedRuleSet,
            HasCondition = hasCondition
        };
    }

    public static SettingsRuleDocument PropertyValidator(string typeName, bool hasCondition, string? ruleSet)
    {
        var shortName = typeName;
        var tick = shortName.IndexOf('`', StringComparison.Ordinal);
        if (tick >= 0)
            shortName = shortName[..tick];

        return new SettingsRuleDocument
        {
            Id = $"Validator:{shortName}",
            Description = $"Должно удовлетворять `{SplitName(shortName)}`.",
            RuleSet = string.IsNullOrWhiteSpace(ruleSet) || ruleSet == "default" ? null : ruleSet,
            HasCondition = hasCondition
        };
    }

    public static bool IsIgnored(string methodName)
        => methodName is "WithMessage" or "WithName" or "WithErrorCode" or "WithSeverity"
            or "WithState" or "OverridePropertyName" or "Configure" or "Cascade"
            or "DependentRules" or "ChildRules" or "SetValidator" or "SetInheritanceValidator"
            or "SetAsyncValidator" or "When" or "Unless" or "WhenAsync" or "UnlessAsync"
            or "WithDisplayName" or "WithErrorList"
            or "Transform" or "TransformForEach" or "ForEach";

    private static (string Id, string Description) Describe(string methodName, IReadOnlyList<string> arguments)
    {
        var arg0 = arguments.ElementAtOrDefault(0) ?? "";
        var arg1 = arguments.ElementAtOrDefault(1) ?? "";
        var arg2 = arguments.ElementAtOrDefault(2) ?? "";

        switch (methodName)
        {
            case "NotNull":
                return ("NotNull", "Не должно быть null.");
            case "NotEmpty":
                return ("NotEmpty", "Не должно быть пустым.");
            case "Null":
                return ("Null", "Должно быть null.");
            case "Empty":
                return ("Empty", "Должно быть пустым.");
            case "MaximumLength":
                return ($"MaximumLength:{arg0}", $"Максимальная длина: {arg0}.");
            case "MinimumLength":
                return ($"MinimumLength:{arg0}", $"Минимальная длина: {arg0}.");
            case "Length" when arguments.Count >= 2:
                return ($"Length:{arg0}-{arg1}", $"Длина должна быть от {arg0} до {arg1}.");
            case "Length":
                return ($"ExactLength:{arg0}", $"Длина должна быть равна {arg0}.");
            case "ExactLength":
                return ($"ExactLength:{arg0}", $"Длина должна быть равна {arg0}.");
            case "InclusiveBetween":
                return ($"InclusiveBetween:{arg0}-{arg1}", $"Значение должно быть от {arg0} до {arg1} включительно.");
            case "ExclusiveBetween":
                return ($"ExclusiveBetween:{arg0}-{arg1}", $"Значение должно быть между {arg0} и {arg1} исключительно.");
            case "GreaterThan":
                return ($"GreaterThan:{arg0}", $"Должно быть больше {arg0}.");
            case "GreaterThanOrEqualTo":
                return ($"GreaterThanOrEqual:{arg0}", $"Должно быть не меньше {arg0}.");
            case "LessThan":
                return ($"LessThan:{arg0}", $"Должно быть меньше {arg0}.");
            case "LessThanOrEqualTo":
                return ($"LessThanOrEqual:{arg0}", $"Должно быть не больше {arg0}.");
            case "EqualTo":
            case "Equal":
                return ($"Equal:{arg0}", $"Должно быть равно {arg0}.");
            case "NotEqual":
                return ($"NotEqual:{arg0}", $"Не должно быть равно {arg0}.");
            case "Matches":
                return ($"Matches:{arg0}", $"Должно соответствовать шаблону `{arg0}`.");
            case "EmailAddress":
                return ("EmailAddress", "Должно быть корректным адресом электронной почты.");
            case "CreditCard":
                return ("CreditCard", "Должно быть корректным номером банковской карты.");
            case "IsInEnum":
                return ("Enum", "Должно быть допустимым значением перечисления.");
            case "IsEnumName":
                {
                    var ignoreCase = IsFalse(arg1);
                    var id = ignoreCase ? $"IsEnumName:{arg0}:ignoreCase" : $"IsEnumName:{arg0}";
                    var description = ignoreCase
                        ? $"Должно быть именем значения перечисления `{arg0}` (без учёта регистра)."
                        : $"Должно быть именем значения перечисления `{arg0}`.";
                    return (id, description);
                }
            case "PrecisionScale":
                {
                    var ignoreZeros = IsTrue(arg2);
                    var id = ignoreZeros ? $"PrecisionScale:{arg0},{arg1},ignoreTrailingZeros" : $"PrecisionScale:{arg0},{arg1}";
                    var description = ignoreZeros
                        ? $"Точность {arg0}, масштаб {arg1} (без учёта хвостовых нулей)."
                        : $"Точность {arg0}, масштаб {arg1}.";
                    return (id, description);
                }
            case "ScalePrecision":
                return ($"ScalePrecision:{arg0},{arg1}", $"Масштаб {arg0}, точность {arg1}.");
            case "Must":
            case "MustAsync":
                return string.IsNullOrWhiteSpace(arg0)
                    ? ("Must", "Должно удовлетворять пользовательскому условию.")
                    : ("Must", $"Должно удовлетворять условию `{arg0}`.");
            case "Custom":
            case "CustomAsync":
                return ("Custom", "Пользовательская проверка.");
            default:
                {
                    var friendly = SplitName(methodName);
                    return (methodName, $"Должно удовлетворять `{friendly}`.");
                }
        }
    }

    private static bool IsTrue(string value)
        => value.Equals("true", StringComparison.OrdinalIgnoreCase);

    private static bool IsFalse(string value)
        => value.Equals("false", StringComparison.OrdinalIgnoreCase);

    private static string SplitName(string name)
    {
        var builder = new StringBuilder();
        foreach (var ch in name)
        {
            if (char.IsUpper(ch) && builder.Length > 0)
                builder.Append(' ');
            builder.Append(ch);
        }

        return builder.ToString();
    }
}
