namespace FluentDocs.Diff;

/// <summary>
/// Compares two settings catalogs using normalized rule identifiers.
/// </summary>
public static class CatalogDiffer
{
    /// <summary>
    /// Returns the changes required to go from <paramref name="previous"/> to <paramref name="current"/>.
    /// When <paramref name="previous"/> is <c>null</c>, the result has no changes (initial catalog).
    /// </summary>
    public static CatalogDiff Diff(SettingsCatalog? previous, SettingsCatalog current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (previous is null)
            return new CatalogDiff();

        var previousTypes = previous.Types.ToDictionary(t => t.FullName, StringComparer.Ordinal);
        var currentTypes = current.Types.ToDictionary(t => t.FullName, StringComparer.Ordinal);

        var diff = new CatalogDiff
        {
            AddedTypes = current.Types.Where(t => !previousTypes.ContainsKey(t.FullName)).ToList(),
            RemovedTypes = previous.Types.Where(t => !currentTypes.ContainsKey(t.FullName)).ToList()
        };

        foreach (var currentType in current.Types)
        {
            if (!previousTypes.TryGetValue(currentType.FullName, out var previousType))
                continue;

            var typeDiff = DiffType(previousType, currentType);
            if (typeDiff.HasChanges)
                diff.ChangedTypes.Add(typeDiff);
        }

        return diff;
    }

    private static TypeDiff DiffType(SettingsTypeDocument previous, SettingsTypeDocument current)
    {
        var previousProperties = previous.Properties.ToDictionary(p => p.Path, StringComparer.Ordinal);
        var currentProperties = current.Properties.ToDictionary(p => p.Path, StringComparer.Ordinal);

        var typeDiff = new TypeDiff
        {
            FullName = current.FullName,
            ConfigurationPath = current.ConfigurationPath,
            ConfigurationPathChange = FormatChange(previous.ConfigurationPath, current.ConfigurationPath),
            SummaryChange = FormatChange(previous.Summary, current.Summary),
            AddedProperties = current.Properties.Where(p => !previousProperties.ContainsKey(p.Path)).ToList(),
            RemovedProperties = previous.Properties.Where(p => !currentProperties.ContainsKey(p.Path)).ToList()
        };

        foreach (var currentProperty in current.Properties)
        {
            if (!previousProperties.TryGetValue(currentProperty.Path, out var previousProperty))
                continue;

            var propertyDiff = DiffProperty(previousProperty, currentProperty);
            if (propertyDiff.HasChanges)
                typeDiff.ChangedProperties.Add(propertyDiff);
        }

        return typeDiff;
    }

    private static PropertyDiff DiffProperty(SettingsPropertyDocument previous, SettingsPropertyDocument current)
    {
        var previousRules = previous.Rules.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var currentRules = current.Rules.ToDictionary(r => r.Id, StringComparer.Ordinal);

        var propertyDiff = new PropertyDiff
        {
            Path = current.Path,
            TypeChange = FormatChange(previous.ClrType, current.ClrType),
            DefaultChange = FormatChange(previous.DefaultValue, current.DefaultValue),
            SummaryChange = FormatChange(previous.Summary, current.Summary),
            AddedRules = current.Rules.Where(r => !previousRules.ContainsKey(r.Id)).ToList(),
            RemovedRules = previous.Rules.Where(r => !currentRules.ContainsKey(r.Id)).ToList()
        };

        foreach (var currentRule in current.Rules)
        {
            if (!previousRules.TryGetValue(currentRule.Id, out var previousRule))
                continue;

            if (!RulesEqual(previousRule, currentRule))
            {
                propertyDiff.ChangedRules.Add(new RuleChange
                {
                    Id = currentRule.Id,
                    Previous = previousRule,
                    Current = currentRule
                });
            }
        }

        return propertyDiff;
    }

    private static bool RulesEqual(SettingsRuleDocument left, SettingsRuleDocument right)
        => string.Equals(left.Description, right.Description, StringComparison.Ordinal)
           && string.Equals(left.Message, right.Message, StringComparison.Ordinal)
           && string.Equals(left.RuleSet, right.RuleSet, StringComparison.Ordinal)
           && left.HasCondition == right.HasCondition;

    private static string? FormatChange(string? previous, string? current)
    {
        if (string.Equals(previous, current, StringComparison.Ordinal))
            return null;

        return $"{Display(previous)} → {Display(current)}";
    }

    private static string Display(string? value)
        => string.IsNullOrEmpty(value) ? "(none)" : value;
}
