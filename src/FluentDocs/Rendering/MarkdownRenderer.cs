using System.Text;

namespace FluentDocs.Rendering;

/// <summary>
/// Формирует Markdown-каталог настроек и журнал изменений.
/// </summary>
public static class MarkdownRenderer
{
    /// <summary>
    /// Собирает итоговый Markdown-файл.
    /// </summary>
    /// <param name="catalog">Текущий каталог настроек.</param>
    /// <param name="diff">Отличия от предыдущего снимка. Игнорируется, если <paramref name="isInitial"/> равен <c>true</c>.</param>
    /// <param name="isInitial"><c>true</c>, если предыдущего снимка ещё не было.</param>
    public static string Render(SettingsCatalog catalog, CatalogDiff? diff, bool isInitial = false)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var builder = new StringBuilder();
        builder.AppendLine("# Настройки приложения");
        builder.AppendLine();
        builder.AppendLine("Сформировано по валидаторам FluentValidation и XML-комментариям к классам настроек.");
        builder.AppendLine();
        builder.AppendLine("## Журнал изменений");
        builder.AppendLine();
        AppendChangelog(builder, diff, isInitial);
        builder.AppendLine("## Каталог");
        builder.AppendLine();

        if (catalog.Types.Count == 0)
        {
            builder.AppendLine("Типы настроек не найдены.");
            builder.AppendLine();
        }
        else
        {
            foreach (var type in catalog.Types)
                AppendType(builder, type);
        }

        if (catalog.Warnings.Count > 0)
        {
            builder.AppendLine("## Предупреждения");
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
            builder.AppendLine("Каталог сформирован впервые.");
            builder.AppendLine();
            return;
        }

        if (!diff.HasChanges)
        {
            builder.AppendLine("Изменений настроек относительно предыдущего снимка нет.");
            builder.AppendLine();
            return;
        }

        if (diff.AddedTypes.Count > 0)
        {
            builder.AppendLine("### Добавленные типы настроек");
            builder.AppendLine();
            foreach (var type in diff.AddedTypes)
                builder.AppendLine($"- `{type.Name}` (`{FormatPath(type)}`)");
            builder.AppendLine();
        }

        if (diff.RemovedTypes.Count > 0)
        {
            builder.AppendLine("### Удалённые типы настроек");
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
                builder.AppendLine($"- **Путь конфигурации:** {type.ConfigurationPathChange}");
            if (type.SummaryChange is not null)
                builder.AppendLine($"- **Описание:** {type.SummaryChange}");
            foreach (var property in type.AddedProperties)
                builder.AppendLine($"- **Добавлено свойство** `{property.Path}` ({property.ClrType})");
            foreach (var property in type.RemovedProperties)
                builder.AppendLine($"- **Удалено свойство** `{property.Path}`");
            foreach (var property in type.ChangedProperties)
            {
                builder.AppendLine($"- **Изменено свойство** `{property.Path}`");
                if (property.TypeChange is not null)
                    builder.AppendLine($"  - Тип: {property.TypeChange}");
                if (property.DefaultChange is not null)
                    builder.AppendLine($"  - Значение по умолчанию: {property.DefaultChange}");
                if (property.SummaryChange is not null)
                    builder.AppendLine($"  - Описание: {property.SummaryChange}");
                foreach (var rule in property.AddedRules)
                    builder.AppendLine($"  - Добавлено ограничение `{rule.Id}`: {rule.Description}");
                foreach (var rule in property.RemovedRules)
                    builder.AppendLine($"  - Удалено ограничение `{rule.Id}`: {rule.Description}");
                foreach (var rule in property.ChangedRules)
                    builder.AppendLine($"  - Изменено ограничение `{rule.Id}`: {rule.Previous.Description} → {rule.Current.Description}");
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

        builder.AppendLine($"**Тип:** `{type.FullName}`");
        builder.AppendLine();

        if (type.Properties.Count == 0)
        {
            builder.AppendLine("_Свойства не найдены._");
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

        builder.AppendLine($"- **Тип:** `{property.ClrType}`");
        if (property.DefaultValue is not null)
            builder.AppendLine($"- **По умолчанию:** `{property.DefaultValue}`");

        if (property.Rules.Count > 0)
        {
            builder.AppendLine("- **Ограничения:**");
            foreach (var rule in property.Rules)
            {
                var condition = rule.HasCondition ? " _(условно)_" : "";
                var ruleSet = rule.RuleSet is null ? "" : $" `[набор правил: {rule.RuleSet}]`";
                builder.AppendLine($"  - {rule.Description}{condition}{ruleSet} (`{rule.Id}`)");
                if (!string.IsNullOrWhiteSpace(rule.Message))
                    builder.AppendLine($"    - Сообщение: {rule.Message}");
            }
        }
        else
        {
            builder.AppendLine("- **Ограничения:** не обнаружены");
        }

        builder.AppendLine();
    }

    private static string FormatPath(SettingsTypeDocument type)
        => string.IsNullOrWhiteSpace(type.ConfigurationPath) ? type.FullName : type.ConfigurationPath;
}
