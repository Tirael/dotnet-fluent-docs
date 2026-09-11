using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentDocs.Rendering;

/// <summary>
/// Serializes catalogs to the canonical JSON snapshot format.
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
    /// Serializes a catalog to indented JSON.
    /// </summary>
    public static string Serialize(SettingsCatalog catalog)
        => JsonSerializer.Serialize(catalog, Options);

    /// <summary>
    /// Deserializes a catalog snapshot.
    /// </summary>
    public static SettingsCatalog Deserialize(string json)
        => JsonSerializer.Deserialize<SettingsCatalog>(json, Options)
           ?? throw new InvalidOperationException("Settings snapshot JSON deserialized to null.");
}
