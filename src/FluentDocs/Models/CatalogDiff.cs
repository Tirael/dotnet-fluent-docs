namespace FluentDocs;

/// <summary>
/// Отличие одного каталога настроек от другого.
/// </summary>
public sealed class CatalogDiff
{
    /// <summary>
    /// Признак любых изменений типов, свойств, комментариев, значений по умолчанию или правил.
    /// </summary>
    public bool HasChanges =>
        AddedTypes.Count > 0
        || RemovedTypes.Count > 0
        || ChangedTypes.Count > 0;

    /// <summary>
    /// Типы, которые есть только в текущем каталоге.
    /// </summary>
    public List<SettingsTypeDocument> AddedTypes { get; set; } = [];

    /// <summary>
    /// Типы, которые есть только в предыдущем каталоге.
    /// </summary>
    public List<SettingsTypeDocument> RemovedTypes { get; set; } = [];

    /// <summary>
    /// Типы, присутствующие в обоих каталогах, у которых изменились свойства или метаданные.
    /// </summary>
    public List<TypeDiff> ChangedTypes { get; set; } = [];
}

/// <summary>
/// Изменения свойств одного типа настроек.
/// </summary>
public sealed class TypeDiff
{
    /// <summary>
    /// Полное имя типа настроек.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Путь конфигурации, если задан.
    /// </summary>
    public string? ConfigurationPath { get; set; }

    /// <summary>
    /// Изменение пути конфигурации в формате <c>старое → новое</c>.
    /// </summary>
    public string? ConfigurationPathChange { get; set; }

    /// <summary>
    /// Изменение описания типа в формате <c>старое → новое</c>.
    /// </summary>
    public string? SummaryChange { get; set; }

    /// <summary>
    /// Свойства, добавленные у этого типа.
    /// </summary>
    public List<SettingsPropertyDocument> AddedProperties { get; set; } = [];

    /// <summary>
    /// Свойства, удалённые у этого типа.
    /// </summary>
    public List<SettingsPropertyDocument> RemovedProperties { get; set; } = [];

    /// <summary>
    /// Свойства, у которых изменились тип, значение по умолчанию, комментарии или правила.
    /// </summary>
    public List<PropertyDiff> ChangedProperties { get; set; } = [];

    /// <summary>
    /// Признак любых задокументированных изменений этого типа.
    /// </summary>
    public bool HasChanges =>
        ConfigurationPathChange is not null
        || SummaryChange is not null
        || AddedProperties.Count > 0
        || RemovedProperties.Count > 0
        || ChangedProperties.Count > 0;
}

/// <summary>
/// Изменения одного свойства.
/// </summary>
public sealed class PropertyDiff
{
    /// <summary>
    /// Dotted-путь свойства.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Изменение CLR-типа в формате <c>старое → новое</c>.
    /// </summary>
    public string? TypeChange { get; set; }

    /// <summary>
    /// Изменение значения по умолчанию в формате <c>старое → новое</c>.
    /// </summary>
    public string? DefaultChange { get; set; }

    /// <summary>
    /// Изменение описания в формате <c>старое → новое</c>.
    /// </summary>
    public string? SummaryChange { get; set; }

    /// <summary>
    /// Правила, добавленные к свойству.
    /// </summary>
    public List<SettingsRuleDocument> AddedRules { get; set; } = [];

    /// <summary>
    /// Правила, удалённые у свойства.
    /// </summary>
    public List<SettingsRuleDocument> RemovedRules { get; set; } = [];

    /// <summary>
    /// Правила с тем же идентификатором, у которых изменились описание или сообщение.
    /// </summary>
    public List<RuleChange> ChangedRules { get; set; } = [];

    /// <summary>
    /// Признак любых задокументированных изменений этого свойства.
    /// </summary>
    public bool HasChanges =>
        TypeChange is not null
        || DefaultChange is not null
        || SummaryChange is not null
        || AddedRules.Count > 0
        || RemovedRules.Count > 0
        || ChangedRules.Count > 0;
}

/// <summary>
/// Правило, идентификатор которого не изменился, но изменился текст.
/// </summary>
public sealed class RuleChange
{
    /// <summary>
    /// Идентификатор правила.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Предыдущее состояние правила.
    /// </summary>
    public SettingsRuleDocument Previous { get; set; } = new();

    /// <summary>
    /// Текущее состояние правила.
    /// </summary>
    public SettingsRuleDocument Current { get; set; } = new();
}
