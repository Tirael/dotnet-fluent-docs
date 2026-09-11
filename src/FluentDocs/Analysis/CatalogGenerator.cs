using System.Collections;
using System.Reflection;

namespace FluentDocs.Analysis;

/// <summary>
/// Builds a <see cref="SettingsCatalog"/> from a compiled assembly and optional XML documentation file.
/// </summary>
public static class CatalogGenerator
{
    private const string SettingsDocsAttributeName = "FluentDocs.SettingsDocsAttribute";
    private const string ValidatorInterfaceName = "IValidator`1";

    /// <summary>
    /// Analyzes validators already loaded in the current load context.
    /// </summary>
    public static SettingsCatalog Generate(Assembly assembly, string? xmlDocumentationPath = null)
        => GenerateCore(assembly, xmlDocumentationPath);

    /// <summary>
    /// Loads <paramref name="assemblyPath"/> in an isolated context and analyzes its validators.
    /// </summary>
    public static SettingsCatalog GenerateFromPath(string assemblyPath, string? xmlDocumentationPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
        var fullPath = Path.GetFullPath(assemblyPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Assembly '{fullPath}' was not found.", fullPath);

        var context = new PluginLoadContext(fullPath);
        try
        {
            var assembly = context.LoadFromAssemblyPath(fullPath);
            return GenerateCore(assembly, xmlDocumentationPath);
        }
        finally
        {
            context.Unload();
        }
    }

    private static SettingsCatalog GenerateCore(Assembly assembly, string? xmlDocumentationPath)
    {
        var xml = XmlDocumentationReader.Load(xmlDocumentationPath);
        var warnings = new List<string>();
        var validators = DiscoverValidators(assembly, warnings);
        var settingsTypes = SelectSettingsTypes(validators);

        var documents = new List<SettingsTypeDocument>();
        foreach (var (modelType, validatorType, validatorInstance) in settingsTypes.OrderBy(x => x.ModelType.FullName, StringComparer.Ordinal))
        {
            documents.Add(BuildTypeDocument(modelType, validatorType, validatorInstance, xml, warnings));
        }

        return new SettingsCatalog
        {
            Version = "1",
            Types = documents,
            Warnings = warnings.Distinct(StringComparer.Ordinal).OrderBy(w => w, StringComparer.Ordinal).ToList()
        };
    }

    private static List<(Type ValidatorType, Type ModelType, object? Instance)> DiscoverValidators(Assembly assembly, List<string> warnings)
    {
        var discovered = new List<(Type ValidatorType, Type ModelType, object? Instance)>();
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract)
                continue;

            var modelType = GetValidatedType(type);
            if (modelType is null)
                continue;

            object? instance = null;
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                warnings.Add($"Could not instantiate validator '{type.FullName}' (parameterless constructor required in v1): {ex.GetBaseException().Message}");
            }

            discovered.Add((type, modelType, instance));
        }

        return discovered;
    }

    private static Type? GetValidatedType(Type validatorType)
    {
        var validatorInterface = validatorType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition().Name == ValidatorInterfaceName);
        return validatorInterface?.GetGenericArguments()[0];
    }

    private static List<(Type ModelType, Type ValidatorType, object? Instance)> SelectSettingsTypes(
        List<(Type ValidatorType, Type ModelType, object? Instance)> validators)
    {
        var attributed = validators
            .Where(v => GetConfigurationPath(v.ModelType) is not null || HasSettingsDocsAttribute(v.ModelType))
            .ToList();

        var selected = attributed.Count > 0
            ? attributed
            : validators.Where(v => LooksLikeSettingsType(v.ModelType)).ToList();

        if (selected.Count == 0)
            selected = validators;

        return selected
            .GroupBy(v => v.ModelType)
            .Select(g =>
            {
                var preferred = g.FirstOrDefault(x => x.Instance is not null);
                var fallback = g.First();
                var chosen = preferred.ValidatorType is not null ? preferred : fallback;
                return (chosen.ModelType, chosen.ValidatorType, chosen.Instance);
            })
            .ToList();
    }

    private static bool HasSettingsDocsAttribute(Type type)
        => type.GetCustomAttributes(inherit: false).Any(a => a.GetType().FullName == SettingsDocsAttributeName);

    private static string? GetConfigurationPath(Type type)
    {
        var attribute = type.GetCustomAttributes(inherit: false)
            .FirstOrDefault(a => a.GetType().FullName == SettingsDocsAttributeName);
        return attribute is null
            ? null
            : ReflectionHelpers.GetPropertyValue<string>(attribute, "ConfigurationPath");
    }

    private static bool LooksLikeSettingsType(Type type)
    {
        var name = type.Name;
        return name.EndsWith("Options", StringComparison.Ordinal)
               || name.EndsWith("Settings", StringComparison.Ordinal)
               || name.EndsWith("Configuration", StringComparison.Ordinal)
               || name.EndsWith("Config", StringComparison.Ordinal);
    }

    private static SettingsTypeDocument BuildTypeDocument(
        Type modelType,
        Type validatorType,
        object? validatorInstance,
        XmlDocumentationReader xml,
        List<string> warnings)
    {
        var typeDocs = xml.Get(ReflectionHelpers.DocumentationId(modelType));
        var document = new SettingsTypeDocument
        {
            FullName = modelType.FullName ?? modelType.Name,
            Name = modelType.Name,
            ConfigurationPath = GetConfigurationPath(modelType),
            Summary = typeDocs?.Summary,
            Remarks = typeDocs?.Remarks
        };

        var defaults = TryCreate(modelType);
        AddDeclaredProperties(modelType, prefix: "", defaults, document, xml);

        if (validatorInstance is not null)
        {
            ExtractRules(validatorInstance, prefix: "", modelType, defaults, document, xml, warnings, []);
        }
        else
        {
            warnings.Add($"Settings type '{modelType.FullName}' has validator '{validatorType.FullName}' that could not be instantiated.");
        }

        document.Properties = document.Properties
            .OrderBy(p => p.Path, StringComparer.Ordinal)
            .Select(p =>
            {
                p.Rules = p.Rules
                    .GroupBy(r => r.Id, StringComparer.Ordinal)
                    .Select(g => g.First())
                    .OrderBy(r => r.Id, StringComparer.Ordinal)
                    .ToList();
                return p;
            })
            .ToList();

        return document;
    }

    private static void AddDeclaredProperties(
        Type type,
        string prefix,
        object? instance,
        SettingsTypeDocument document,
        XmlDocumentationReader xml)
    {
        foreach (var property in GetPublicProperties(type))
        {
            var path = CombinePath(prefix, property.Name, collection: false);
            EnsureProperty(document, path, property, instance, xml);
        }
    }

    private static void ExtractRules(
        object validator,
        string prefix,
        Type modelType,
        object? defaults,
        SettingsTypeDocument document,
        XmlDocumentationReader xml,
        List<string> warnings,
        HashSet<Type> chain)
    {
        var validatorType = validator.GetType();
        if (!chain.Add(validatorType))
            return;

        object? descriptor;
        try
        {
            descriptor = ReflectionHelpers.Invoke(validator, "CreateDescriptor");
        }
        catch (Exception ex)
        {
            warnings.Add($"CreateDescriptor failed for '{validatorType.FullName}': {ex.GetBaseException().Message}");
            return;
        }

        if (descriptor is null)
            return;

        if (ReflectionHelpers.GetPropertyValue(descriptor, "Rules") is not IEnumerable rules)
            return;

        foreach (var rule in rules)
        {
            ProcessRule(rule, prefix, modelType, defaults, document, xml, warnings, chain);
        }
    }

    private static void ProcessRule(
        object rule,
        string prefix,
        Type modelType,
        object? defaults,
        SettingsTypeDocument document,
        XmlDocumentationReader xml,
        List<string> warnings,
        HashSet<Type> chain)
    {
        var propertyName = ReflectionHelpers.GetPropertyValue<string>(rule, "PropertyName");
        var member = ReflectionHelpers.GetPropertyValue(rule, "Member") as MemberInfo;
        var typeToValidate = ReflectionHelpers.GetPropertyValue(rule, "TypeToValidate") as Type;
        var isCollectionRule = ReflectionHelpers.GetNonGenericName(rule.GetType()) == "CollectionPropertyRule";
        var ruleHasCondition = ReflectionHelpers.GetPropertyValue<bool>(rule, "HasCondition")
                               || ReflectionHelpers.GetPropertyValue<bool>(rule, "HasAsyncCondition");
        var ruleSets = ReflectionHelpers.GetPropertyValue(rule, "RuleSets") as string[];
        var ruleSet = ruleSets is { Length: > 0 } ? string.Join(",", ruleSets.Where(s => !string.IsNullOrWhiteSpace(s) && s != "default")) : null;

        PropertyInfo? property = member as PropertyInfo
                                 ?? (propertyName is null ? null : modelType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

        var path = string.IsNullOrWhiteSpace(propertyName)
            ? prefix
            : CombinePath(prefix, propertyName, isCollectionRule);

        if (!string.IsNullOrWhiteSpace(path) && property is not null)
            EnsureProperty(document, path, property, defaults, xml, typeToValidate, isCollectionRule);

        if (ReflectionHelpers.GetPropertyValue(rule, "Components") is IEnumerable components)
        {
            foreach (var component in components)
            {
                var propertyValidator = ReflectionHelpers.GetPropertyValue(component, "Validator");
                if (propertyValidator is null)
                    continue;

                if (TryGetChildValidator(propertyValidator, warnings) is { } child)
                {
                    var childModel = typeToValidate
                                     ?? (isCollectionRule ? ReflectionHelpers.GetCollectionElementType(property?.PropertyType ?? modelType) : property?.PropertyType)
                                     ?? child.GetType();
                    var childPrefix = string.IsNullOrWhiteSpace(path) ? prefix : path;
                    ExtractRules(child, childPrefix, childModel, defaults, document, xml, warnings, [.. chain]);
                    continue;
                }

                var humanized = RuleHumanizer.Humanize(propertyValidator, component, ruleHasCondition, ruleSet);
                if (humanized is null || string.IsNullOrWhiteSpace(path))
                    continue;

                var target = document.Properties.FirstOrDefault(p => p.Path == path);
                if (target is null)
                {
                    target = new SettingsPropertyDocument
                    {
                        Path = path,
                        ClrType = typeToValidate is null ? "object" : ReflectionHelpers.FormatClrType(typeToValidate)
                    };
                    document.Properties.Add(target);
                }

                if (target.Rules.All(r => r.Id != humanized.Id))
                    target.Rules.Add(humanized);
            }
        }

        if (ReflectionHelpers.GetPropertyValue(rule, "DependentRules") is IEnumerable dependentRules)
        {
            var dependentPrefix = string.IsNullOrWhiteSpace(path) ? prefix : path;
            var dependentModel = isCollectionRule
                ? ReflectionHelpers.GetCollectionElementType(property?.PropertyType ?? typeToValidate ?? modelType) ?? modelType
                : property?.PropertyType ?? modelType;
            foreach (var dependent in dependentRules)
            {
                ProcessRule(dependent, dependentPrefix, dependentModel, defaults, document, xml, warnings, chain);
            }
        }
    }

    private static object? TryGetChildValidator(object propertyValidator, List<string> warnings)
    {
        if (!ReflectionHelpers.ImplementsInterface(propertyValidator, "IChildValidatorAdaptor")
            && ReflectionHelpers.GetNonGenericName(propertyValidator.GetType()) != "ChildValidatorAdaptor")
        {
            return null;
        }

        if (ReflectionHelpers.GetPropertyValue(propertyValidator, "ValidatorType") is not Type validatorType)
            return null;

        try
        {
            return Activator.CreateInstance(validatorType);
        }
        catch (Exception ex)
        {
            warnings.Add($"Could not instantiate child validator '{validatorType.FullName}': {ex.GetBaseException().Message}");
            return null;
        }
    }

    private static void EnsureProperty(
        SettingsTypeDocument document,
        string path,
        PropertyInfo property,
        object? instance,
        XmlDocumentationReader xml,
        Type? typeOverride = null,
        bool collectionRule = false)
    {
        var existing = document.Properties.FirstOrDefault(p => p.Path == path);
        var clrType = collectionRule && ReflectionHelpers.GetCollectionElementType(property.PropertyType) is { } element
            ? $"{ReflectionHelpers.FormatClrType(element)}[]"
            : ReflectionHelpers.FormatClrType(typeOverride ?? property.PropertyType);
        var docs = xml.Get(ReflectionHelpers.DocumentationId(property));
        var defaultValue = FormatDefaultForPath(instance, path);

        if (existing is null)
        {
            document.Properties.Add(new SettingsPropertyDocument
            {
                Path = path,
                ClrType = clrType,
                DefaultValue = defaultValue,
                Summary = docs?.Summary,
                Remarks = docs?.Remarks
            });
            return;
        }

        if (string.IsNullOrWhiteSpace(existing.Summary))
            existing.Summary = docs?.Summary;
        if (string.IsNullOrWhiteSpace(existing.Remarks))
            existing.Remarks = docs?.Remarks;
        existing.DefaultValue ??= defaultValue;
        if (string.IsNullOrWhiteSpace(existing.ClrType) || existing.ClrType == "object")
            existing.ClrType = clrType;
    }

    private static IEnumerable<PropertyInfo> GetPublicProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

    private static object? TryCreate(Type type)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }

    private static string? FormatDefaultForPath(object? root, string path)
    {
        if (root is null)
            return null;

        object? current = root;
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
        {
            if (current is null)
                return null;

            var segment = segments[i];
            var collection = segment.EndsWith("[]", StringComparison.Ordinal);
            var name = collection ? segment[..^2] : segment;
            current = current.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(current);
            if (collection)
                return i == segments.Length - 1 ? ReflectionHelpers.FormatDefaultValue(current) : null;
        }

        return ReflectionHelpers.FormatDefaultValue(current);
    }

    private static string CombinePath(string prefix, string name, bool collection)
    {
        var segment = collection ? $"{name}[]" : name;
        return string.IsNullOrWhiteSpace(prefix) ? segment : $"{prefix}.{segment}";
    }
}
