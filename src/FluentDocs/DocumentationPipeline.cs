using Microsoft.CodeAnalysis;

namespace FluentDocs;

/// <summary>
/// Входные данные для генерации файлов документации настроек.
/// </summary>
public sealed class DocumentationRequest
{
    /// <summary>
    /// Исходные файлы C#, которые нужно разобрать через Roslyn.
    /// </summary>
    public IReadOnlyList<string> SourceFiles { get; init; } = [];

    /// <summary>
    /// Ссылки на сборки, необходимые для семантической модели.
    /// </summary>
    public IReadOnlyList<string> References { get; init; } = [];

    /// <summary>
    /// Уже собранная компиляция. Если задана, списки файлов не используются.
    /// </summary>
    public Compilation? Compilation { get; init; }

    /// <summary>
    /// Необязательный XML-файл документации компилятора.
    /// </summary>
    public string? XmlDocumentationPath { get; init; }

    /// <summary>
    /// Путь к итоговому Markdown-файлу.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Путь к JSON-снимку. Если файл уже есть, он используется как предыдущий снимок.
    /// </summary>
    public string? SnapshotPath { get; init; }

    /// <summary>
    /// Явный путь к предыдущему снимку. По умолчанию совпадает с <see cref="SnapshotPath"/>, если тот файл существует.
    /// </summary>
    public string? PreviousSnapshotPath { get; init; }
}

/// <summary>
/// Собирает каталог, считает журнал изменений и записывает файлы.
/// </summary>
public static class DocumentationPipeline
{
    /// <summary>
    /// Генерирует файлы документации и возвращает результат в памяти.
    /// </summary>
    public static DocumentationResult Run(DocumentationRequest request, TextWriter? log = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Compilation is null && request.SourceFiles.Count == 0)
            throw new ArgumentException("Нужно указать Compilation или SourceFiles.", nameof(request));
        log ??= TextWriter.Null;

        var catalog = request.Compilation is not null
            ? CatalogGenerator.Generate(request.Compilation, request.XmlDocumentationPath)
            : CatalogGenerator.GenerateFromFiles(request.SourceFiles, request.References, request.XmlDocumentationPath);
        var previousPath = ResolvePreviousSnapshotPath(request);
        SettingsCatalog? previous = null;
        if (previousPath is not null && File.Exists(previousPath))
            previous = SnapshotSerializer.ReadFromFile(previousPath);

        var isInitial = previous is null;
        var diff = CatalogDiffer.Diff(previous, catalog);
        var markdown = MarkdownRenderer.Render(catalog, diff, isInitial);

        WriteAll(request.OutputPath, markdown);
        if (!string.IsNullOrWhiteSpace(request.SnapshotPath))
            SnapshotSerializer.WriteToFile(request.SnapshotPath, catalog);

        log.WriteLine($"FluentDocs: записан {request.OutputPath}");
        if (!string.IsNullOrWhiteSpace(request.SnapshotPath))
            log.WriteLine($"FluentDocs: записан {request.SnapshotPath}");

        return new DocumentationResult(catalog, diff, markdown, isInitial);
    }

    private static string? ResolvePreviousSnapshotPath(DocumentationRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.PreviousSnapshotPath))
            return request.PreviousSnapshotPath;
        return string.IsNullOrWhiteSpace(request.SnapshotPath) ? null : request.SnapshotPath;
    }

    private static void WriteAll(string path, string contents)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(fullPath, contents, SnapshotSerializer.Utf8Bom);
    }
}

/// <summary>
/// Результат генерации документации в памяти.
/// </summary>
public sealed record DocumentationResult(
    SettingsCatalog Catalog,
    CatalogDiff Diff,
    string Markdown,
    bool IsInitial);
