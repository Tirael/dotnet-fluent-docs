using System.Globalization;
using System.Reflection;
using System.Text;

namespace FluentDocs.Analysis;

internal static class RuleHumanizer
{
    public static SettingsRuleDocument? Humanize(object propertyValidator, object component, bool ruleHasCondition, string? ruleSet)
    {
        var typeName = ReflectionHelpers.GetNonGenericName(propertyValidator.GetType());
        if (typeName is "ChildValidatorAdaptor" or "NoopPropertyValidator" or "PolymorphicValidator")
            return null;

        var name = ReflectionHelpers.GetPropertyValue<string>(propertyValidator, "Name") ?? typeName;
        var (id, description) = Describe(propertyValidator, typeName, name);
        var message = GetCustomMessage(propertyValidator, component);
        var hasCondition = ruleHasCondition
                           || ReflectionHelpers.GetPropertyValue<bool>(component, "HasCondition")
                           || ReflectionHelpers.GetPropertyValue<bool>(component, "HasAsyncCondition");

        var normalizedRuleSet = string.IsNullOrWhiteSpace(ruleSet) || ruleSet == "default" ? null : ruleSet;

        return new SettingsRuleDocument
        {
            Id = id,
            Description = description,
            Message = message,
            RuleSet = normalizedRuleSet,
            HasCondition = hasCondition
        };
    }

    private static (string Id, string Description) Describe(object validator, string typeName, string name)
    {
        switch (typeName)
        {
            case "NotNullValidator":
                return ("NotNull", "Не должно быть null.");
            case "NotEmptyValidator":
                return ("NotEmpty", "Не должно быть пустым.");
            case "NullValidator":
                return ("Null", "Должно быть null.");
            case "EmptyValidator":
                return ("Empty", "Должно быть пустым.");
            case "MaximumLengthValidator":
                {
                    var max = GetInt(validator, "Max");
                    return ($"MaximumLength:{max}", $"Максимальная длина: {max}.");
                }
            case "MinimumLengthValidator":
                {
                    var min = GetInt(validator, "Min");
                    return ($"MinimumLength:{min}", $"Минимальная длина: {min}.");
                }
            case "ExactLengthValidator":
                {
                    var length = GetInt(validator, "Max");
                    return ($"ExactLength:{length}", $"Длина должна быть равна {length}.");
                }
            case "LengthValidator":
                {
                    var min = GetInt(validator, "Min");
                    var max = GetInt(validator, "Max");
                    return ($"Length:{min}-{max}", $"Длина должна быть от {min} до {max}.");
                }
            case "InclusiveBetweenValidator":
                {
                    var from = FormatCompare(GetProperty(validator, "From"));
                    var to = FormatCompare(GetProperty(validator, "To"));
                    return ($"InclusiveBetween:{from}-{to}", $"Значение должно быть от {from} до {to} включительно.");
                }
            case "ExclusiveBetweenValidator":
                {
                    var from = FormatCompare(GetProperty(validator, "From"));
                    var to = FormatCompare(GetProperty(validator, "To"));
                    return ($"ExclusiveBetween:{from}-{to}", $"Значение должно быть между {from} и {to} исключительно.");
                }
            case "GreaterThanValidator":
                {
                    var value = FormatCompare(GetProperty(validator, "ValueToCompare"));
                    return ($"GreaterThan:{value}", $"Должно быть больше {value}.");
                }
            case "GreaterThanOrEqualValidator":
                {
                    var value = FormatCompare(GetProperty(validator, "ValueToCompare"));
                    return ($"GreaterThanOrEqual:{value}", $"Должно быть не меньше {value}.");
                }
            case "LessThanValidator":
                {
                    var value = FormatCompare(GetProperty(validator, "ValueToCompare"));
                    return ($"LessThan:{value}", $"Должно быть меньше {value}.");
                }
            case "LessThanOrEqualValidator":
                {
                    var value = FormatCompare(GetProperty(validator, "ValueToCompare"));
                    return ($"LessThanOrEqual:{value}", $"Должно быть не больше {value}.");
                }
            case "EqualValidator":
                {
                    var value = FormatCompare(GetProperty(validator, "ValueToCompare"));
                    return ($"Equal:{value}", $"Должно быть равно {value}.");
                }
            case "NotEqualValidator":
                {
                    var value = FormatCompare(GetProperty(validator, "ValueToCompare"));
                    return ($"NotEqual:{value}", $"Не должно быть равно {value}.");
                }
            case "RegularExpressionValidator":
                {
                    var expression = GetProperty(validator, "Expression")?.ToString() ?? GetProperty(validator, "Regex")?.ToString() ?? "";
                    return ($"Matches:{expression}", $"Должно соответствовать шаблону `{expression}`.");
                }
            case "AspNetCoreCompatibleEmailValidator":
            case "EmailValidator":
                return ("EmailAddress", "Должно быть корректным адресом электронной почты.");
            case "CreditCardValidator":
                return ("CreditCard", "Должно быть корректным номером банковской карты.");
            case "EnumValidator":
                return ("Enum", "Должно быть допустимым значением перечисления.");
            case "ScalePrecisionValidator":
                {
                    var scale = GetInt(validator, "Scale");
                    var precision = GetInt(validator, "Precision");
                    return ($"ScalePrecision:{scale},{precision}", $"Масштаб {scale}, точность {precision}.");
                }
            case "PredicateValidator":
            case "AsyncPredicateValidator":
                {
                    return ("Must", "Должно удовлетворять пользовательскому условию.");
                }
            default:
                {
                    if (Implements(validator, "INotEmptyValidator"))
                        return ("NotEmpty", "Не должно быть пустым.");
                    if (Implements(validator, "INotNullValidator"))
                        return ("NotNull", "Не должно быть null.");
                    if (Implements(validator, "IEmailValidator"))
                        return ("EmailAddress", "Должно быть корректным адресом электронной почты.");
                    if (Implements(validator, "IRegularExpressionValidator"))
                    {
                        var expression = GetProperty(validator, "Expression")?.ToString() ?? "";
                        return ($"Matches:{expression}", $"Должно соответствовать шаблону `{expression}`.");
                    }

                    var friendly = SplitName(string.IsNullOrWhiteSpace(name) ? typeName : name);
                    return (typeName, $"Должно удовлетворять `{friendly}`.");
                }
        }
    }

    private static string? GetCustomMessage(object propertyValidator, object component)
    {
        try
        {
            var method = component.GetType().GetMethod("GetUnformattedErrorMessage", BindingFlags.Public | BindingFlags.Instance);
            var raw = method?.Invoke(component, null) as string;
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var propertyValidatorInterface = propertyValidator.GetType()
                .GetInterfaces()
                .FirstOrDefault(i => i.Name == "IPropertyValidator" && i.GetMethod("GetDefaultMessageTemplate") is not null);
            var defaultTemplate = propertyValidatorInterface?
                .GetMethod("GetDefaultMessageTemplate")
                ?.Invoke(propertyValidator, [""]) as string;

            if (!string.IsNullOrWhiteSpace(defaultTemplate) && string.Equals(raw, defaultTemplate, StringComparison.Ordinal))
                return null;

            return raw;
        }
        catch
        {
            return null;
        }
    }

    private static bool Implements(object instance, string interfaceName)
        => ReflectionHelpers.ImplementsInterface(instance, interfaceName);

    private static object? GetProperty(object instance, string name)
        => ReflectionHelpers.GetPropertyValue(instance, name);

    private static int GetInt(object instance, string name)
    {
        var value = GetProperty(instance, name);
        return value switch
        {
            int i => i,
            IConvertible convertible => convertible.ToInt32(CultureInfo.InvariantCulture),
            _ => 0
        };
    }

    private static string FormatCompare(object? value)
        => ReflectionHelpers.FormatDefaultValue(value)?.Trim('"') ?? "null";

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
