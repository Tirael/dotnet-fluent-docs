namespace FluentDocs;

/// <summary>
/// Marks a type as application settings that FluentDocs should document.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SettingsDocsAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute.
    /// </summary>
    /// <param name="configurationPath">Configuration section path, for example <c>Mail</c> or <c>Storage:S3</c>.</param>
    public SettingsDocsAttribute(string? configurationPath = null)
    {
        ConfigurationPath = configurationPath;
    }

    /// <summary>
    /// Gets the configuration section path bound to this settings type.
    /// </summary>
    public string? ConfigurationPath { get; }
}
