namespace FluentDocs;

/// <summary>
/// Difference between two settings catalogs.
/// </summary>
public sealed class CatalogDiff
{
    /// <summary>
    /// True when any type, property, comment, default, or rule changed.
    /// </summary>
    public bool HasChanges =>
        AddedTypes.Count > 0
        || RemovedTypes.Count > 0
        || ChangedTypes.Count > 0;

    /// <summary>
    /// Settings types present only in the current catalog.
    /// </summary>
    public List<SettingsTypeDocument> AddedTypes { get; set; } = [];

    /// <summary>
    /// Settings types present only in the previous catalog.
    /// </summary>
    public List<SettingsTypeDocument> RemovedTypes { get; set; } = [];

    /// <summary>
    /// Settings types present in both catalogs with property or metadata changes.
    /// </summary>
    public List<TypeDiff> ChangedTypes { get; set; } = [];
}

/// <summary>
/// Property-level changes for one settings type.
/// </summary>
public sealed class TypeDiff
{
    /// <summary>
    /// Full name of the settings type.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Configuration path, if any.
    /// </summary>
    public string? ConfigurationPath { get; set; }

    /// <summary>
    /// Display-name change, formatted as <c>old → new</c>.
    /// </summary>
    public string? ConfigurationPathChange { get; set; }

    /// <summary>
    /// Type summary comment change, formatted as <c>old → new</c>.
    /// </summary>
    public string? SummaryChange { get; set; }

    /// <summary>
    /// Properties added on this type.
    /// </summary>
    public List<SettingsPropertyDocument> AddedProperties { get; set; } = [];

    /// <summary>
    /// Properties removed from this type.
    /// </summary>
    public List<SettingsPropertyDocument> RemovedProperties { get; set; } = [];

    /// <summary>
    /// Properties whose type, default, comments, or rules changed.
    /// </summary>
    public List<PropertyDiff> ChangedProperties { get; set; } = [];

    /// <summary>
    /// True when this type has any documented change.
    /// </summary>
    public bool HasChanges =>
        ConfigurationPathChange is not null
        || SummaryChange is not null
        || AddedProperties.Count > 0
        || RemovedProperties.Count > 0
        || ChangedProperties.Count > 0;
}

/// <summary>
/// Changes for a single property path.
/// </summary>
public sealed class PropertyDiff
{
    /// <summary>
    /// Dotted property path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// CLR type change, formatted as <c>old → new</c>.
    /// </summary>
    public string? TypeChange { get; set; }

    /// <summary>
    /// Default value change, formatted as <c>old → new</c>.
    /// </summary>
    public string? DefaultChange { get; set; }

    /// <summary>
    /// Summary comment change, formatted as <c>old → new</c>.
    /// </summary>
    public string? SummaryChange { get; set; }

    /// <summary>
    /// Rules added to the property.
    /// </summary>
    public List<SettingsRuleDocument> AddedRules { get; set; } = [];

    /// <summary>
    /// Rules removed from the property.
    /// </summary>
    public List<SettingsRuleDocument> RemovedRules { get; set; } = [];

    /// <summary>
    /// Rules with the same identifier whose description or message changed.
    /// </summary>
    public List<RuleChange> ChangedRules { get; set; } = [];

    /// <summary>
    /// True when this property has any documented change.
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
/// A rule whose identifier stayed the same but whose text changed.
/// </summary>
public sealed class RuleChange
{
    /// <summary>
    /// Rule identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Previous rule snapshot.
    /// </summary>
    public SettingsRuleDocument Previous { get; set; } = new();

    /// <summary>
    /// Current rule snapshot.
    /// </summary>
    public SettingsRuleDocument Current { get; set; } = new();
}
