namespace FluentDocs;

/// <summary>
/// Канонический снимок задокументированных настроек приложения.
/// </summary>
public sealed class SettingsCatalog
{
    /// <summary>
    /// Версия схемы снимка.
    /// </summary>
    public string Version { get; set; } = "1";

    /// <summary>
    /// Типы настроек, отсортированные по полному имени.
    /// </summary>
    public List<SettingsTypeDocument> Types { get; set; } = [];

    /// <summary>
    /// Нефатальные проблемы, возникшие при разборе исходников валидаторов.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// Описание одного типа настроек.
/// </summary>
public sealed class SettingsTypeDocument
{
    /// <summary>
    /// Полное имя типа без сборки, например <c>DemoApp.Options.MailOptions</c>.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Короткое имя типа.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Путь секции конфигурации из <c>[SettingsDocs]</c>.
    /// </summary>
    public string? ConfigurationPath { get; set; }

    /// <summary>
    /// XML <c>summary</c> типа.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// XML <c>remarks</c> типа.
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Свойства, включая вложенные dotted-пути.
    /// </summary>
    public List<SettingsPropertyDocument> Properties { get; set; } = [];
}

/// <summary>
/// Описание одного свойства настроек.
/// </summary>
public sealed class SettingsPropertyDocument
{
    /// <summary>
    /// Dotted-путь от корня настроек; для элементов коллекции используется <c>[]</c>.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Отображаемое имя CLR-типа.
    /// </summary>
    public string ClrType { get; set; } = string.Empty;

    /// <summary>
    /// Отформатированное значение по умолчанию из инициализатора свойства.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// XML <c>summary</c> свойства.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// XML <c>remarks</c> свойства.
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Правила FluentValidation, отсортированные по идентификатору.
    /// </summary>
    public List<SettingsRuleDocument> Rules { get; set; } = [];
}

/// <summary>
/// Одно нормализованное ограничение FluentValidation.
/// </summary>
public sealed class SettingsRuleDocument
{
    /// <summary>
    /// Стабильный идентификатор для сравнения в журнале изменений, например <c>MaximumLength:50</c>.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Человекочитаемое описание ограничения.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Шаблон <c>WithMessage</c>, если он отличается от стандартного сообщения FluentValidation.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Имя набора правил, если правило не входит в набор по умолчанию.
    /// </summary>
    public string? RuleSet { get; set; }

    /// <summary>
    /// Признак условия <c>When</c>/<c>Unless</c> у правила или компонента.
    /// </summary>
    public bool HasCondition { get; set; }
}
