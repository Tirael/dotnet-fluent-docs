using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FluentDocs.Analysis;

/// <summary>
/// Строит <see cref="SettingsCatalog"/> по исходникам валидаторов через Roslyn.
/// </summary>
public static class CatalogGenerator
{
    /// <summary>
    /// Анализирует уже собранную компиляцию Roslyn.
    /// </summary>
    public static SettingsCatalog Generate(Compilation compilation, string? xmlDocumentationPath = null)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        return GenerateCore(compilation, xmlDocumentationPath);
    }

    /// <summary>
    /// Собирает компиляцию из списков файлов и анализирует валидаторы.
    /// </summary>
    public static SettingsCatalog GenerateFromFiles(
        IEnumerable<string> sourceFiles,
        IEnumerable<string> metadataReferences,
        string? xmlDocumentationPath = null)
    {
        var compilation = CompilationFactory.Create(sourceFiles, metadataReferences);
        return GenerateCore(compilation, xmlDocumentationPath);
    }

    private static SettingsCatalog GenerateCore(Compilation compilation, string? xmlDocumentationPath)
    {
        var xml = XmlDocumentationReader.Load(xmlDocumentationPath);
        var warnings = new List<string>();
        var validators = DiscoverValidators(compilation);

        if (validators.Count == 0 && compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            warnings.Add("Компиляция исходников содержит ошибки; валидаторы FluentValidation не найдены.");

        var selected = SelectSettingsTypes(validators);
        var extractor = new ValidatorRuleExtractor(compilation, xml, warnings);
        var documents = new List<SettingsTypeDocument>();

        foreach (var (modelType, validatorType) in selected.OrderBy(x => x.ModelType.ToDisplayString(), StringComparer.Ordinal))
            documents.Add(BuildTypeDocument(modelType, validatorType, xml, extractor, compilation));

        return new SettingsCatalog
        {
            Version = "1",
            Types = documents,
            Warnings = warnings.Distinct(StringComparer.Ordinal).OrderBy(w => w, StringComparer.Ordinal).ToList()
        };
    }

    private static List<(INamedTypeSymbol ValidatorType, ITypeSymbol ModelType)> DiscoverValidators(Compilation compilation)
    {
        var discovered = new List<(INamedTypeSymbol ValidatorType, ITypeSymbol ModelType)>();
        foreach (var type in EnumerateNamedTypes(compilation.GlobalNamespace))
        {
            if (type.TypeKind != TypeKind.Class || type.IsAbstract)
                continue;

            var modelType = ValidatorRuleExtractor.GetValidatedType(type);
            if (modelType is null)
                continue;

            discovered.Add((type, modelType));
        }

        return discovered;
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNamedTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            yield return type;
            foreach (var nested in EnumerateNested(type))
                yield return nested;
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            foreach (var type in EnumerateNamedTypes(child))
                yield return type;
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNested(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;
            foreach (var deeper in EnumerateNested(nested))
                yield return deeper;
        }
    }

    private static List<(ITypeSymbol ModelType, INamedTypeSymbol ValidatorType)> SelectSettingsTypes(
        List<(INamedTypeSymbol ValidatorType, ITypeSymbol ModelType)> validators)
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
            .GroupBy(v => v.ModelType.ToDisplayString(), StringComparer.Ordinal)
            .Select(g =>
            {
                var items = g.ToList();
                var preferred = items.FirstOrDefault(x => x.ValidatorType.Name == x.ModelType.Name + "Validator");
                var chosen = preferred.ValidatorType is not null ? preferred : items[0];
                return (chosen.ModelType, chosen.ValidatorType);
            })
            .ToList();
    }

    private static bool HasSettingsDocsAttribute(ITypeSymbol type)
        => type.GetAttributes().Any(IsSettingsDocsAttribute);

    private static string? GetConfigurationPath(ITypeSymbol type)
    {
        var attribute = type.GetAttributes().FirstOrDefault(IsSettingsDocsAttribute);
        if (attribute is null)
            return null;

        if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string path)
            return path;

        var named = attribute.NamedArguments.FirstOrDefault(p => p.Key == "ConfigurationPath");
        return named.Value.Value as string;
    }

    private static bool IsSettingsDocsAttribute(AttributeData attribute)
    {
        var name = attribute.AttributeClass?.Name;
        return name is "SettingsDocsAttribute" or "SettingsDocs";
    }

    private static bool LooksLikeSettingsType(ITypeSymbol type)
    {
        var name = type.Name;
        return name.EndsWith("Options", StringComparison.Ordinal)
               || name.EndsWith("Settings", StringComparison.Ordinal)
               || name.EndsWith("Configuration", StringComparison.Ordinal)
               || name.EndsWith("Config", StringComparison.Ordinal);
    }

    private static SettingsTypeDocument BuildTypeDocument(
        ITypeSymbol modelType,
        INamedTypeSymbol validatorType,
        XmlDocumentationReader xml,
        ValidatorRuleExtractor extractor,
        Compilation compilation)
    {
        var typeDocs = xml.Get(modelType);
        var document = new SettingsTypeDocument
        {
            FullName = modelType.ToDisplayString(),
            Name = modelType.Name,
            ConfigurationPath = GetConfigurationPath(modelType),
            Summary = typeDocs?.Summary,
            Remarks = typeDocs?.Remarks
        };

        AddDeclaredProperties(modelType, document, xml, compilation);
        extractor.Extract(validatorType, prefix: "", modelType, document, []);

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
        ITypeSymbol type,
        SettingsTypeDocument document,
        XmlDocumentationReader xml,
        Compilation compilation)
    {
        foreach (var property in GetPublicProperties(type))
            EnsureProperty(document, property.Name, property, property.Type, collectionRule: false, xml, compilation);
    }

    internal static void EnsureProperty(
        SettingsTypeDocument document,
        string path,
        IPropertySymbol property,
        ITypeSymbol currentType,
        bool collectionRule,
        XmlDocumentationReader xml,
        Compilation compilation)
    {
        var existing = document.Properties.FirstOrDefault(p => p.Path == path);
        var clrType = collectionRule && TypeSymbolFormatter.GetCollectionElementType(property.Type) is { } element
            ? $"{TypeSymbolFormatter.Format(element)}[]"
            : TypeSymbolFormatter.Format(collectionRule ? currentType : property.Type);
        var docs = xml.Get(property);
        var defaultValue = FormatDefault(property, path, compilation);

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

    private static IEnumerable<IPropertySymbol> GetPublicProperties(ITypeSymbol type)
        => type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public
                        && !p.IsStatic
                        && p.GetMethod is not null
                        && p.Parameters.Length == 0);

    private static string? FormatDefault(IPropertySymbol property, string path, Compilation compilation)
    {
        if (path.Contains("[]", StringComparison.Ordinal) && !path.EndsWith("[]", StringComparison.Ordinal))
            return null;

        return FormatPropertyDefault(property, compilation);
    }

    private static string? FormatPropertyDefault(IPropertySymbol property, Compilation compilation)
    {
        foreach (var syntaxRef in property.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is not PropertyDeclarationSyntax declaration || declaration.Initializer is null)
                continue;

            var expression = declaration.Initializer.Value;
            var model = compilation.GetSemanticModel(declaration.SyntaxTree);
            var constant = model.GetConstantValue(expression);
            if (constant.HasValue)
                return ValueFormatter.FormatConstant(constant.Value);

            if (IsEmptyCollectionExpression(expression) && TypeSymbolFormatter.IsCollection(property.Type))
                return "[]";

            if (expression is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax)
                return TypeSymbolFormatter.IsCollection(property.Type) && IsEmptyCreation(expression) ? "[]" : null;
        }

        return property.Type.IsValueType ? ValueFormatter.FormatTypeDefault(property.Type) : null;
    }

    private static bool IsEmptyCollectionExpression(ExpressionSyntax expression)
        => expression is CollectionExpressionSyntax { Elements.Count: 0 }
           || expression is ArrayCreationExpressionSyntax { Initializer: null or { Expressions.Count: 0 } }
           || expression is ImplicitArrayCreationExpressionSyntax { Initializer.Expressions.Count: 0 };

    private static bool IsEmptyCreation(ExpressionSyntax expression)
        => expression switch
        {
            ObjectCreationExpressionSyntax creation => creation.Initializer is null or { Expressions.Count: 0 }
                                                       && (creation.ArgumentList is null || creation.ArgumentList.Arguments.Count == 0),
            ImplicitObjectCreationExpressionSyntax creation => creation.Initializer is null or { Expressions.Count: 0 }
                                                              && (creation.ArgumentList is null || creation.ArgumentList.Arguments.Count == 0),
            _ => false
        };
}
