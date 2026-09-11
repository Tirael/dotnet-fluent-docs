namespace FluentDocs;

/// <summary>
/// Помечает тип как настройки приложения, которые FluentDocs должен задокументировать.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SettingsDocsAttribute : Attribute
{
    /// <summary>
    /// Создаёт атрибут.
    /// </summary>
    /// <param name="configurationPath">Путь секции конфигурации, например <c>Mail</c> или <c>Storage:S3</c>.</param>
    public SettingsDocsAttribute(string? configurationPath = null)
    {
        ConfigurationPath = configurationPath;
    }

    /// <summary>
    /// Путь секции конфигурации, к которой привязан этот тип настроек.
    /// </summary>
    public string? ConfigurationPath { get; }
}
