using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentDocs.Rendering;

/// <summary>
/// Сериализует каталоги в канонический JSON-снимок.
/// </summary>
public static class SnapshotSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Сериализует каталог в форматированный JSON.
    /// </summary>
    public static string Serialize(SettingsCatalog catalog)
        => JsonSerializer.Serialize(catalog, Options);

    /// <summary>
    /// Десериализует снимок каталога.
    /// </summary>
    public static SettingsCatalog Deserialize(string json)
        => JsonSerializer.Deserialize<SettingsCatalog>(json, Options)
           ?? throw new InvalidOperationException("JSON-снимок настроек десериализовался в null.");
}
