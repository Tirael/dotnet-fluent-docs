using System.Text;

namespace FluentDocs.Rendering;

/// <summary>
/// Renders a settings catalog and optional changelog to Markdown.
/// </summary>
public static class MarkdownRenderer
{
    /// <summary>
    /// Renders Markdown documentation.
    /// </summary>
    /// <param name="catalog">Current catalog.</param>
    /// <param name="diff">Diff against the previous snapshot. Ignored when <paramref name="isInitial"/> is true.</param>
    /// <param name="isInitial">True when no previous snapshot existed.</param>
    public static string Render(SettingsCatalog catalog, CatalogDiff? diff, bool isInitial = false)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var builder = new StringBuilder();
        builder.AppendLine("# Application settings");
        builder.AppendLine();
        builder.AppendLine("Generated from FluentValidation validators and XML documentation comments.");
        builder.AppendLine();
        builder.AppendLine("## Changelog");
        builder.AppendLine();
        AppendChangelog(builder, diff, isInitial);
        builder.AppendLine("## Catalog");
        builder.AppendLine();

        if (catalog.Types.Count == 0)
        {
            builder.AppendLine("No settings types were discovered.");
            builder.AppendLine();
        }
        else
        {
            foreach (var type in catalog.Types)
                AppendType(builder, type);
        }

        if (catalog.Warnings.Count > 0)
        {
            builder.AppendLine("## Warnings");
            builder.AppendLine();
            foreach (var warning in catalog.Warnings)
                builder.AppendLine($"- {warning}");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void AppendChangelog(StringBuilder builder, CatalogDiff? diff, bool isInitial)
    {
        if (isInitial || diff is null)
        {
            builder.AppendLine("Initial catalog generated.");
            builder.AppendLine();
            return;
        }

        if (!diff.HasChanges)
        {
            builder.AppendLine("No settings changes since the previous snapshot.");
            builder.AppendLine();
            return;
        }

        if (diff.AddedTypes.Count > 0)
        {
            builder.AppendLine("### Added settings types");
            builder.AppendLine();
            foreach (var type in diff.AddedTypes)
                builder.AppendLine($"- `{type.Name}` (`{FormatPath(type)}`)");
            builder.AppendLine();
        }

        if (diff.RemovedTypes.Count > 0)
        {
            builder.AppendLine("### Removed settings types");
            builder.AppendLine();
            foreach (var type in diff.RemovedTypes)
                builder.AppendLine($"- `{type.Name}` (`{FormatPath(type)}`)");
            builder.AppendLine();
        }

        foreach (var type in diff.ChangedTypes)
        {
            builder.AppendLine($"### `{type.FullName}`");
            builder.AppendLine();
            if (type.ConfigurationPathChange is not null)
                builder.AppendLine($"- **Configuration path:** {type.ConfigurationPathChange}");
            if (type.SummaryChange is not null)
                builder.AppendLine($"- **Summary:** {type.SummaryChange}");
            foreach (var property in type.AddedProperties)
                builder.AppendLine($"- **Added property** `{property.Path}` ({property.ClrType})");
            foreach (var property in type.RemovedProperties)
                builder.AppendLine($"- **Removed property** `{property.Path}`");
            foreach (var property in type.ChangedProperties)
            {
                builder.AppendLine($"- **Changed property** `{property.Path}`");
                if (property.TypeChange is not null)
                    builder.AppendLine($"  - Type: {property.TypeChange}");
                if (property.DefaultChange is not null)
                    builder.AppendLine($"  - Default: {property.DefaultChange}");
                if (property.SummaryChange is not null)
                    builder.AppendLine($"  - Summary: {property.SummaryChange}");
                foreach (var rule in property.AddedRules)
                    builder.AppendLine($"  - Added constraint `{rule.Id}`: {rule.Description}");
                foreach (var rule in property.RemovedRules)
                    builder.AppendLine($"  - Removed constraint `{rule.Id}`: {rule.Description}");
                foreach (var rule in property.ChangedRules)
                    builder.AppendLine($"  - Changed constraint `{rule.Id}`: {rule.Previous.Description} → {rule.Current.Description}");
            }

            builder.AppendLine();
        }
    }

    private static void AppendType(StringBuilder builder, SettingsTypeDocument type)
    {
        var heading = string.IsNullOrWhiteSpace(type.ConfigurationPath)
            ? type.Name
            : $"{type.Name} (`{type.ConfigurationPath}`)";
        builder.AppendLine($"### {heading}");
        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(type.Summary))
        {
            builder.AppendLine(type.Summary);
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(type.Remarks))
        {
            builder.AppendLine(type.Remarks);
            builder.AppendLine();
        }

        builder.AppendLine($"**Type:** `{type.FullName}`");
        builder.AppendLine();

        if (type.Properties.Count == 0)
        {
            builder.AppendLine("_No properties discovered._");
            builder.AppendLine();
            return;
        }

        foreach (var property in type.Properties)
            AppendProperty(builder, property);
    }

    private static void AppendProperty(StringBuilder builder, SettingsPropertyDocument property)
    {
        builder.AppendLine($"#### {property.Path}");
        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(property.Summary))
        {
            builder.AppendLine(property.Summary);
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(property.Remarks))
        {
            builder.AppendLine(property.Remarks);
            builder.AppendLine();
        }

        builder.AppendLine($"- **Type:** `{property.ClrType}`");
        if (property.DefaultValue is not null)
            builder.AppendLine($"- **Default:** `{property.DefaultValue}`");

        if (property.Rules.Count > 0)
        {
            builder.AppendLine("- **Constraints:**");
            foreach (var rule in property.Rules)
            {
                var condition = rule.HasCondition ? " _(conditional)_" : "";
                var ruleSet = rule.RuleSet is null ? "" : $" `[ruleset: {rule.RuleSet}]`";
                builder.AppendLine($"  - {rule.Description}{condition}{ruleSet} (`{rule.Id}`)");
                if (!string.IsNullOrWhiteSpace(rule.Message))
                    builder.AppendLine($"    - Message: {rule.Message}");
            }
        }
        else
        {
            builder.AppendLine("- **Constraints:** none discovered");
        }

        builder.AppendLine();
    }

    private static string FormatPath(SettingsTypeDocument type)
        => string.IsNullOrWhiteSpace(type.ConfigurationPath) ? type.FullName : type.ConfigurationPath;
}
