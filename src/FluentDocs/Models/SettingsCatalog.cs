namespace FluentDocs;

/// <summary>
/// Canonical snapshot of documented application settings.
/// </summary>
public sealed class SettingsCatalog
{
    /// <summary>
    /// Snapshot schema version.
    /// </summary>
    public string Version { get; set; } = "1";

    /// <summary>
    /// Documented settings types, sorted by full name.
    /// </summary>
    public List<SettingsTypeDocument> Types { get; set; } = [];

    /// <summary>
    /// Non-fatal issues encountered while analyzing validators.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>
/// Documentation for one settings type.
/// </summary>
public sealed class SettingsTypeDocument
{
    /// <summary>
    /// Assembly-qualified-style full name without assembly, e.g. <c>DemoApp.Options.MailOptions</c>.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Short type name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Configuration section path from <c>[SettingsDocs]</c>.
    /// </summary>
    public string? ConfigurationPath { get; set; }

    /// <summary>
    /// XML <c>summary</c> for the type.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// XML <c>remarks</c> for the type.
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Properties including nested dotted paths.
    /// </summary>
    public List<SettingsPropertyDocument> Properties { get; set; } = [];
}

/// <summary>
/// Documentation for one settings property.
/// </summary>
public sealed class SettingsPropertyDocument
{
    /// <summary>
    /// Dotted path from the settings root, using <c>[]</c> for collection elements.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Display CLR type name.
    /// </summary>
    public string ClrType { get; set; } = string.Empty;

    /// <summary>
    /// Formatted default value, if it could be read from a parameterless constructor.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// XML <c>summary</c> for the property.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// XML <c>remarks</c> for the property.
    /// </summary>
    public string? Remarks { get; set; }

    /// <summary>
    /// Validation rules from FluentValidation, sorted by identifier.
    /// </summary>
    public List<SettingsRuleDocument> Rules { get; set; } = [];
}

/// <summary>
/// One normalized FluentValidation constraint.
/// </summary>
public sealed class SettingsRuleDocument
{
    /// <summary>
    /// Stable identifier used for changelog comparison, e.g. <c>MaximumLength:50</c>.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable constraint description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Custom <c>WithMessage</c> template when it differs from the FluentValidation default.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Rule set name when the rule is not in the default set.
    /// </summary>
    public string? RuleSet { get; set; }

    /// <summary>
    /// True when the rule or component has a <c>When</c>/<c>Unless</c> condition.
    /// </summary>
    public bool HasCondition { get; set; }
}
