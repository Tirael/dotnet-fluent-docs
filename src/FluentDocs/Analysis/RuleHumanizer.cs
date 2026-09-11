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
                return ("NotNull", "Must not be null.");
            case "NotEmptyValidator":
                return ("NotEmpty", "Must not be empty.");
            case "NullValidator":
                return ("Null", "Must be null.");
            case "EmptyValidator":
                return ("Empty", "Must be empty.");
            case "MaximumLengthValidator":
                {
                    var max = GetInt(validator, "Max");
                    return ($"MaximumLength:{max}", $"Maximum length is {max}.");
                }
            case "MinimumLengthValidator":
                {
                    var min = GetInt(validator, "Min");
                    return ($"MinimumLength:{min}", $"Minimum length is {min}.");
                }
            case "ExactLengthValidator":
                {
                    var length = GetInt(validator, "Max");
                    return ($"ExactLength:{length}", $"Length must be {length}.");
                }
            case "LengthValidator":
                {
                    var min = GetInt(validator, "Min");
                    var max = GetInt(validator, "Max");
                    return ($"Length:{min}-{max}", $"Length must be between {min} and {max}.");
                }
            case "InclusiveBetweenValidator":
                {
                    var from = FormatCompare(GetProperty(validator, "From"));
                    var to = FormatCompare(GetProperty(validator, "To"));
                    return ($"InclusiveBetween:{from}-{to}", $"Must be between {from} and {to} (inclusive).");
                }
            case "ExclusiveBetweenValidator":
                {
                    var from = FormatCompare(GetProperty(validator, "From"));
                    var to = FormatCompare(GetProperty(validator, "To"));
                    return ($"ExclusiveBetween:{from}-{to}", $"Must be between {from} and {to} (exclusive).");
                }
            case "GreaterThanValidator":
                {
                    var value = FormatCompare(GetProperty(validator, "ValueToCompare"));
                    return ($"GreaterThan:{value}", $"Must be greater than {value}.");
                }
            case "GreaterThanOrEqualValidator":
                {
                    var value = FormatCompare(GetProperty(validator, "ValueToCompare"));
                    return ($"GreaterThanOrEqual:{value}", $"Must be greater than or equal to {value}.");
                }
            case "LessThanValidator":
                {
                    var value = FormatCompare(GetProperty(validator, "ValueToCompare"));
                    return ($"LessThan:{value}", $"Must be less than {value}.");
                }
            case "LessThanOrEqualValidator":
                {
                    var value = FormatCompare(GetProperty(validator, "ValueToCompare"));
                    return ($"LessThanOrEqual:{value}", $"Must be less than or equal to {value}.");
                }
            case "EqualValidator":
                {
                    var value = FormatCompare(GetProperty(validator, "ValueToCompare"));
                    return ($"Equal:{value}", $"Must equal {value}.");
                }
            case "NotEqualValidator":
                {
                    var value = FormatCompare(GetProperty(validator, "ValueToCompare"));
                    return ($"NotEqual:{value}", $"Must not equal {value}.");
                }
            case "RegularExpressionValidator":
                {
                    var expression = GetProperty(validator, "Expression")?.ToString() ?? GetProperty(validator, "Regex")?.ToString() ?? "";
                    return ($"Matches:{expression}", $"Must match pattern `{expression}`.");
                }
            case "AspNetCoreCompatibleEmailValidator":
            case "EmailValidator":
                return ("EmailAddress", "Must be a valid email address.");
            case "CreditCardValidator":
                return ("CreditCard", "Must be a valid credit card number.");
            case "EnumValidator":
                return ("Enum", "Must be a valid enum value.");
            case "ScalePrecisionValidator":
                {
                    var scale = GetInt(validator, "Scale");
                    var precision = GetInt(validator, "Precision");
                    return ($"ScalePrecision:{scale},{precision}", $"Must have a scale of {scale} and precision of {precision}.");
                }
            case "PredicateValidator":
            case "AsyncPredicateValidator":
                {
                    return ("Must", "Must satisfy a custom predicate.");
                }
            default:
                {
                    if (Implements(validator, "INotEmptyValidator"))
                        return ("NotEmpty", "Must not be empty.");
                    if (Implements(validator, "INotNullValidator"))
                        return ("NotNull", "Must not be null.");
                    if (Implements(validator, "IEmailValidator"))
                        return ("EmailAddress", "Must be a valid email address.");
                    if (Implements(validator, "IRegularExpressionValidator"))
                    {
                        var expression = GetProperty(validator, "Expression")?.ToString() ?? "";
                        return ($"Matches:{expression}", $"Must match pattern `{expression}`.");
                    }

                    var friendly = SplitName(string.IsNullOrWhiteSpace(name) ? typeName : name);
                    return (typeName, $"Must satisfy `{friendly}`.");
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
