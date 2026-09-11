using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FluentDocs.Analysis;

/// <summary>
/// Обходит конструкторы и вспомогательные методы <c>AbstractValidator&lt;T&gt;</c> и собирает правила.
/// </summary>
internal sealed class ValidatorRuleExtractor
{
    private readonly Compilation _compilation;
    private readonly XmlDocumentationReader _xml;
    private readonly List<string> _warnings;
    private readonly Dictionary<SyntaxTree, SemanticModel> _models = new();

    public ValidatorRuleExtractor(Compilation compilation, XmlDocumentationReader xml, List<string> warnings)
    {
        _compilation = compilation;
        _xml = xml;
        _warnings = warnings;
    }

    public void Extract(
        INamedTypeSymbol validatorType,
        string prefix,
        ITypeSymbol modelType,
        SettingsTypeDocument document,
        HashSet<string> chain,
        bool hasCondition = false,
        string? ruleSet = null)
    {
        if (!chain.Add(validatorType.ToDisplayString()))
            return;

        var visitedMethods = new HashSet<string>(StringComparer.Ordinal);
        for (var type = validatorType; type is not null && !IsFluentValidationType(type); type = type.BaseType)
        {
            foreach (var ctor in type.Constructors.Where(c => !c.IsStatic))
                VisitMethod(ctor, prefix, modelType, document, chain, hasCondition, ruleSet, visitedMethods);
        }
    }

    private void VisitMethod(
        IMethodSymbol method,
        string prefix,
        ITypeSymbol modelType,
        SettingsTypeDocument document,
        HashSet<string> chain,
        bool hasCondition,
        string? ruleSet,
        HashSet<string> visitedMethods)
    {
        if (method.DeclaringSyntaxReferences.Length == 0)
            return;
        if (!visitedMethods.Add(method.ToDisplayString()))
            return;

        foreach (var syntaxRef in method.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax();
            var model = GetModel(syntax.SyntaxTree);
            foreach (var invocation in EnumerateBodyInvocations(syntax))
                ProcessInvocation(invocation, prefix, modelType, document, chain, hasCondition, ruleSet, visitedMethods, model);
        }
    }

    private void WalkLambda(
        LambdaExpressionSyntax lambda,
        string prefix,
        ITypeSymbol modelType,
        SettingsTypeDocument document,
        HashSet<string> chain,
        bool hasCondition,
        string? ruleSet,
        HashSet<string> visitedMethods,
        SemanticModel model)
    {
        switch (lambda.Body)
        {
            case BlockSyntax block:
                foreach (var statement in block.Statements)
                {
                    if (statement is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation })
                        ProcessInvocation(invocation, prefix, modelType, document, chain, hasCondition, ruleSet, visitedMethods, model);
                }

                break;
            case InvocationExpressionSyntax invocation:
                ProcessInvocation(invocation, prefix, modelType, document, chain, hasCondition, ruleSet, visitedMethods, model);
                break;
        }
    }

    private void ProcessInvocation(
        InvocationExpressionSyntax invocation,
        string prefix,
        ITypeSymbol modelType,
        SettingsTypeDocument document,
        HashSet<string> chain,
        bool hasCondition,
        string? ruleSet,
        HashSet<string> visitedMethods,
        SemanticModel model)
    {
        var chainCalls = Flatten(invocation);
        if (chainCalls.Count == 0)
            return;

        var root = chainCalls[0];
        var rootName = GetInvokedName(root);

        switch (rootName)
        {
            case "When" or "Unless" or "WhenAsync" or "UnlessAsync":
                foreach (var lambda in GetActionLambdas(root))
                    WalkLambda(lambda, prefix, modelType, document, chain, hasCondition: true, ruleSet, visitedMethods, model);
                return;
            case "RuleSet":
                {
                    var name = EvaluateArgument(root, 0, model) ?? "";
                    var nestedSet = string.IsNullOrWhiteSpace(name) ? ruleSet : name;
                    foreach (var lambda in GetLambdas(root))
                        WalkLambda(lambda, prefix, modelType, document, chain, hasCondition, nestedSet, visitedMethods, model);
                    return;
                }
            case "Include":
                {
                    if (ResolveValidatorType(root, model) is { } included)
                        Extract(included, prefix, modelType, document, [.. chain], hasCondition, ruleSet);
                    else
                        _warnings.Add($"Не удалось разобрать Include в '{GetLocation(root)}'.");
                    return;
                }
            case "RuleFor" or "RuleForEach":
                ProcessRuleChain(chainCalls, prefix, modelType, document, chain, hasCondition, ruleSet, visitedMethods, model, rootName == "RuleForEach");
                return;
            default:
                TryFollowHelper(root, prefix, modelType, document, chain, hasCondition, ruleSet, visitedMethods, model);
                return;
        }
    }

    private void ProcessRuleChain(
        IReadOnlyList<InvocationExpressionSyntax> calls,
        string prefix,
        ITypeSymbol modelType,
        SettingsTypeDocument document,
        HashSet<string> chain,
        bool hasCondition,
        string? ruleSet,
        HashSet<string> visitedMethods,
        SemanticModel model,
        bool collectionRule)
    {
        var ruleFor = calls[0];
        var member = BindMemberPath(ruleFor, modelType, collectionRule);
        var path = CombinePath(prefix, member.Path, collectionRule && !member.Path.EndsWith("[]", StringComparison.Ordinal));
        var currentModel = member.CurrentType ?? modelType;
        var property = member.Property;

        if (!string.IsNullOrWhiteSpace(path) && property is not null)
            CatalogGenerator.EnsureProperty(document, path, property, currentModel, collectionRule, _xml, _compilation);

        SettingsRuleDocument? lastRule = null;
        var ruleCondition = hasCondition;
        var target = string.IsNullOrWhiteSpace(path) ? null : document.Properties.FirstOrDefault(p => p.Path == path);

        foreach (var call in calls.Skip(1))
        {
            var name = GetInvokedName(call);
            switch (name)
            {
                case "WithMessage":
                    if (lastRule is not null)
                        lastRule.Message = EvaluateArgument(call, 0, model) ?? lastRule.Message;
                    continue;
                case "When" or "Unless" or "WhenAsync" or "UnlessAsync":
                    ruleCondition = true;
                    if (lastRule is not null)
                        lastRule.HasCondition = true;
                    continue;
                case "SetValidator" or "SetInheritanceValidator":
                    {
                        if (ResolveValidatorType(call, model) is { } childValidator)
                        {
                            var childModel = collectionRule
                                ? TypeSymbolFormatter.GetCollectionElementType(property?.Type ?? currentModel) ?? currentModel
                                : property?.Type ?? currentModel;
                            var childPrefix = string.IsNullOrWhiteSpace(path) ? prefix : path;
                            Extract(childValidator, childPrefix, TypeSymbolFormatter.UnwrapNullable(childModel), document, [.. chain], ruleCondition, ruleSet);
                        }
                        else
                        {
                            _warnings.Add($"Не удалось найти вложенный валидатор в '{GetLocation(call)}'.");
                        }

                        lastRule = null;
                        continue;
                    }
                case "ChildRules" or "DependentRules":
                    {
                        var childPrefix = string.IsNullOrWhiteSpace(path) ? prefix : path;
                        var childModel = collectionRule
                            ? TypeSymbolFormatter.GetCollectionElementType(property?.Type ?? currentModel) ?? currentModel
                            : name == "DependentRules"
                                ? currentModel
                                : property?.Type ?? currentModel;
                        foreach (var lambda in GetLambdas(call))
                            WalkLambda(lambda, childPrefix, TypeSymbolFormatter.UnwrapNullable(childModel), document, chain, ruleCondition, ruleSet, visitedMethods, model);
                        lastRule = null;
                        continue;
                    }
                case var ignored when RuleHumanizer.IsIgnored(ignored):
                    continue;
            }

            if (string.IsNullOrWhiteSpace(path))
                continue;

            target ??= document.Properties.FirstOrDefault(p => p.Path == path);
            if (target is null)
            {
                target = new SettingsPropertyDocument
                {
                    Path = path,
                    ClrType = collectionRule && TypeSymbolFormatter.GetCollectionElementType(property?.Type ?? currentModel) is { } element
                        ? $"{TypeSymbolFormatter.Format(element)}[]"
                        : TypeSymbolFormatter.Format(member.MemberType ?? property?.Type)
                };
                document.Properties.Add(target);
            }

            var args = EvaluateArguments(call, model, name);
            var humanized = RuleHumanizer.Humanize(name, args, message: null, ruleCondition, ruleSet);
            if (humanized is null)
                continue;

            if (target.Rules.All(r => r.Id != humanized.Id))
                target.Rules.Add(humanized);
            lastRule = target.Rules.First(r => r.Id == humanized.Id);
        }
    }

    private void TryFollowHelper(
        InvocationExpressionSyntax invocation,
        string prefix,
        ITypeSymbol modelType,
        SettingsTypeDocument document,
        HashSet<string> chain,
        bool hasCondition,
        string? ruleSet,
        HashSet<string> visitedMethods,
        SemanticModel model)
    {
        var symbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol
                     ?? model.GetSymbolInfo(invocation).CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
        if (symbol is null || symbol.MethodKind != MethodKind.Ordinary)
            return;
        if (IsFluentValidationType(symbol.ContainingType))
            return;

        VisitMethod(symbol, prefix, modelType, document, chain, hasCondition, ruleSet, visitedMethods);
    }

    private static MemberBinding BindMemberPath(
        InvocationExpressionSyntax ruleFor,
        ITypeSymbol modelType,
        bool collectionRule)
    {
        if (ruleFor.ArgumentList.Arguments.Count == 0)
            return new MemberBinding("", modelType, null, null);

        var expression = UnwrapLambda(ruleFor.ArgumentList.Arguments[0].Expression);
        var names = new List<string>();
        CollectNames(expression, names);
        if (names.Count == 0)
            return new MemberBinding("", modelType, null, null);

        ITypeSymbol current = modelType;
        IPropertySymbol? last = null;
        foreach (var name in names)
        {
            if (FindProperty(current, name) is not { } property)
                continue;

            last = property;
            current = TypeSymbolFormatter.UnwrapNullable(property.Type);
        }

        if (collectionRule)
            names[^1] += "[]";

        var path = string.Join(".", names);
        var memberType = collectionRule
            ? TypeSymbolFormatter.GetCollectionElementType(last?.Type ?? current) ?? current
            : last?.Type ?? current;
        return new MemberBinding(path, TypeSymbolFormatter.UnwrapNullable(memberType), last, last?.Type);
    }

    private static void CollectNames(ExpressionSyntax? expression, List<string> names)
    {
        switch (expression)
        {
            case MemberAccessExpressionSyntax access:
                CollectNames(access.Expression, names);
                names.Add(access.Name.Identifier.Text);
                break;
            case MemberBindingExpressionSyntax binding:
                names.Add(binding.Name.Identifier.Text);
                break;
            case ConditionalAccessExpressionSyntax conditional:
                CollectNames(conditional.Expression, names);
                CollectNames(conditional.WhenNotNull, names);
                break;
        }
    }

    private static IPropertySymbol? FindProperty(ITypeSymbol type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var property = current.GetMembers(name).OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Parameters.Length == 0 && !p.IsStatic);
            if (property is not null)
                return property;
        }

        return null;
    }

    private INamedTypeSymbol? ResolveValidatorType(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (GetGenericTypeArgument(invocation) is { } genericName)
        {
            var type = model.GetTypeInfo(genericName).Type as INamedTypeSymbol
                       ?? model.GetSymbolInfo(genericName).Symbol as INamedTypeSymbol;
            if (type is not null)
                return type;
        }

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            var expression = argument.Expression;
            if (expression is LambdaExpressionSyntax lambda)
                expression = UnwrapLambda(lambda) ?? expression;

            var created = FindCreatedType(expression, model);
            if (created is not null)
                return created;

            if (model.GetTypeInfo(expression).Type is INamedTypeSymbol typed && IsValidatorType(typed))
                return typed;
        }

        return null;
    }

    private static INamedTypeSymbol? FindCreatedType(ExpressionSyntax expression, SemanticModel model)
    {
        return expression switch
        {
            ObjectCreationExpressionSyntax creation => model.GetTypeInfo(creation).Type as INamedTypeSymbol
                                                        ?? model.GetSymbolInfo(creation.Type).Symbol as INamedTypeSymbol,
            ImplicitObjectCreationExpressionSyntax implicitCreation => model.GetTypeInfo(implicitCreation).Type as INamedTypeSymbol,
            InvocationExpressionSyntax inner => FindCreatedType(inner, model),
            _ => expression.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>()
                .Select(n => model.GetTypeInfo(n).Type as INamedTypeSymbol)
                .FirstOrDefault(t => t is not null)
        };
    }

    private static TypeSyntax? GetGenericTypeArgument(InvocationExpressionSyntax invocation)
    {
        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax access => access.Name,
            GenericNameSyntax generic => generic,
            _ => null
        };

        return name is GenericNameSyntax g && g.TypeArgumentList.Arguments.Count > 0
            ? g.TypeArgumentList.Arguments[0]
            : null;
    }

    private List<string> EvaluateArguments(InvocationExpressionSyntax invocation, SemanticModel model, string methodName)
    {
        var values = new List<string>();
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.Expression is LambdaExpressionSyntax)
            {
                if (methodName is "Must" or "MustAsync" or "Equal" or "EqualTo" or "NotEqual"
                    or "GreaterThan" or "GreaterThanOrEqualTo" or "LessThan" or "LessThanOrEqualTo")
                {
                    var path = MemberPathFromLambda(argument.Expression);
                    if (!string.IsNullOrWhiteSpace(path))
                        values.Add(path);
                }

                continue;
            }

            var constant = model.GetConstantValue(argument.Expression);
            if (constant.HasValue)
            {
                values.Add(ValueFormatter.FormatCompare(constant.Value));
                continue;
            }

            values.Add(CollapseWhitespace(argument.Expression.ToString()));
        }

        return values;
    }

    private string? EvaluateArgument(InvocationExpressionSyntax invocation, int index, SemanticModel model)
    {
        if (index >= invocation.ArgumentList.Arguments.Count)
            return null;

        var expression = invocation.ArgumentList.Arguments[index].Expression;
        var constant = model.GetConstantValue(expression);
        if (constant.HasValue)
            return constant.Value as string ?? ValueFormatter.FormatCompare(constant.Value);

        return CollapseWhitespace(expression.ToString().Trim('"'));
    }

    private static string? MemberPathFromLambda(ExpressionSyntax expression)
    {
        var body = UnwrapLambda(expression);
        var names = new List<string>();
        while (body is MemberAccessExpressionSyntax access)
        {
            names.Add(access.Name.Identifier.Text);
            body = access.Expression;
        }

        names.Reverse();
        return names.Count == 0 ? null : string.Join(".", names);
    }

    private static ExpressionSyntax? UnwrapLambda(ExpressionSyntax expression)
        => expression switch
        {
            SimpleLambdaExpressionSyntax { Body: ExpressionSyntax body } => body,
            ParenthesizedLambdaExpressionSyntax { Body: ExpressionSyntax body } => body,
            _ => expression
        };

    private static IEnumerable<LambdaExpressionSyntax> GetLambdas(InvocationExpressionSyntax invocation)
        => invocation.ArgumentList.Arguments.Select(a => a.Expression).OfType<LambdaExpressionSyntax>();

    private static IEnumerable<LambdaExpressionSyntax> GetActionLambdas(InvocationExpressionSyntax invocation)
    {
        var lambdas = GetLambdas(invocation).ToList();
        return lambdas.Count <= 1 ? lambdas : lambdas.Skip(1);
    }

    private static IEnumerable<InvocationExpressionSyntax> EnumerateBodyInvocations(SyntaxNode syntax)
    {
        var expressionBody = syntax switch
        {
            ConstructorDeclarationSyntax ctor => ctor.ExpressionBody,
            MethodDeclarationSyntax method => method.ExpressionBody,
            LocalFunctionStatementSyntax local => local.ExpressionBody,
            _ => null
        };
        if (expressionBody?.Expression is InvocationExpressionSyntax arrowInvocation)
        {
            yield return arrowInvocation;
            yield break;
        }

        var block = syntax switch
        {
            ConstructorDeclarationSyntax ctor => ctor.Body,
            MethodDeclarationSyntax method => method.Body,
            LocalFunctionStatementSyntax local => local.Body,
            _ => null
        };
        if (block is null)
            yield break;

        foreach (var statement in block.Statements)
        {
            if (statement is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation })
                yield return invocation;
        }
    }

    private static List<InvocationExpressionSyntax> Flatten(InvocationExpressionSyntax invocation)
    {
        var list = new List<InvocationExpressionSyntax>();
        ExpressionSyntax? current = invocation;
        while (current is InvocationExpressionSyntax inv)
        {
            list.Add(inv);
            current = inv.Expression switch
            {
                MemberAccessExpressionSyntax access => access.Expression,
                _ => null
            };
        }

        list.Reverse();
        return list;
    }

    private static string GetInvokedName(InvocationExpressionSyntax invocation)
        => invocation.Expression switch
        {
            MemberAccessExpressionSyntax access => access.Name.Identifier.Text,
            MemberBindingExpressionSyntax binding => binding.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            _ => ""
        };

    private SemanticModel GetModel(SyntaxTree tree)
    {
        if (!_models.TryGetValue(tree, out var model))
        {
            model = _compilation.GetSemanticModel(tree);
            _models[tree] = model;
        }

        return model;
    }

    internal static bool IsValidatorType(INamedTypeSymbol type)
        => GetValidatedType(type) is not null;

    internal static ITypeSymbol? GetValidatedType(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.Name == "AbstractValidator" && current.Arity == 1)
                return current.TypeArguments[0];
        }

        foreach (var iface in type.AllInterfaces)
        {
            if (iface.Name == "IValidator" && iface.Arity == 1)
                return iface.TypeArguments[0];
        }

        return null;
    }

    internal static bool IsFluentValidationType(INamedTypeSymbol? type)
    {
        if (type is null)
            return false;
        var ns = type.ContainingNamespace?.ToDisplayString();
        return ns is "FluentValidation" or "FluentValidation.Validators" or "FluentValidation.Internal";
    }

    internal static string CombinePath(string prefix, string name, bool collection)
    {
        if (string.IsNullOrWhiteSpace(name))
            return prefix;
        var segment = collection && !name.EndsWith("[]", StringComparison.Ordinal) ? $"{name}[]" : name;
        return string.IsNullOrWhiteSpace(prefix) ? segment : $"{prefix}.{segment}";
    }

    private static string GetLocation(SyntaxNode node)
    {
        var line = node.GetLocation().GetLineSpan();
        return $"{Path.GetFileName(line.Path)}:{line.StartLinePosition.Line + 1}";
    }

    private static string CollapseWhitespace(string value)
        => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private readonly record struct MemberBinding(
        string Path,
        ITypeSymbol CurrentType,
        IPropertySymbol? Property,
        ITypeSymbol? MemberType);
}
