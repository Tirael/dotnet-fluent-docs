using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentDocs.Rendering;

/// <summary>
/// Сериализует каталоги в канонический JSON-снимок.
/// </summary>
public static class SnapshotSerializer
{
    /// <summary>
    /// UTF-8 с BOM, чтобы редакторы на Windows открывали кириллицу корректно.
    /// </summary>
    public static readonly Encoding Utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Сериализует каталог в форматированный JSON с читаемой кириллицей.
    /// </summary>
    public static string Serialize(SettingsCatalog catalog)
        => JsonSerializer.Serialize(catalog, Options);

    /// <summary>
    /// Записывает снимок в файл в UTF-8 с BOM, без Unicode-escape для кириллицы.
    /// </summary>
    public static void WriteToFile(string path, SettingsCatalog catalog)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(fullPath, Serialize(catalog), Utf8Bom);
    }

    /// <summary>
    /// Читает снимок из JSON-файла.
    /// </summary>
    public static SettingsCatalog ReadFromFile(string path)
        => Deserialize(File.ReadAllText(path, Encoding.UTF8));

    /// <summary>
    /// Десериализует снимок каталога.
    /// </summary>
    public static SettingsCatalog Deserialize(string json)
        => JsonSerializer.Deserialize<SettingsCatalog>(json, Options)
           ?? throw new InvalidOperationException("JSON-снимок настроек десериализовался в null.");
}
