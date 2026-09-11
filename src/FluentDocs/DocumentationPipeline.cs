namespace FluentDocs;

/// <summary>
/// Inputs for generating settings documentation files.
/// </summary>
public sealed class DocumentationRequest
{
    /// <summary>
    /// Path to the compiled application assembly. Required when <see cref="Assembly"/> is not provided.
    /// </summary>
    public string AssemblyPath { get; init; } = string.Empty;

    /// <summary>
    /// Optional already-loaded assembly. When set, the pipeline skips isolated load-context loading.
    /// </summary>
    public System.Reflection.Assembly? Assembly { get; init; }

    /// <summary>
    /// Optional XML documentation file produced by the compiler.
    /// </summary>
    public string? XmlDocumentationPath { get; init; }

    /// <summary>
    /// Markdown output path.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// JSON snapshot path written after generation. When the file already exists it is used as the previous snapshot.
    /// </summary>
    public string? SnapshotPath { get; init; }

    /// <summary>
    /// Optional explicit previous snapshot. Defaults to <see cref="SnapshotPath"/> when that file exists.
    /// </summary>
    public string? PreviousSnapshotPath { get; init; }
}

/// <summary>
/// Orchestrates catalog generation, changelog computation, and file writes.
/// </summary>
public static class DocumentationPipeline
{
    /// <summary>
    /// Generates documentation files and returns the in-memory results.
    /// </summary>
    public static DocumentationResult Run(DocumentationRequest request, TextWriter? log = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Assembly is null && string.IsNullOrWhiteSpace(request.AssemblyPath))
            throw new ArgumentException("Assembly or AssemblyPath is required.", nameof(request));
        log ??= TextWriter.Null;

        var catalog = request.Assembly is not null
            ? CatalogGenerator.Generate(request.Assembly, request.XmlDocumentationPath)
            : CatalogGenerator.GenerateFromPath(request.AssemblyPath, request.XmlDocumentationPath);
        var previousPath = ResolvePreviousSnapshotPath(request);
        SettingsCatalog? previous = null;
        if (previousPath is not null && File.Exists(previousPath))
            previous = SnapshotSerializer.Deserialize(File.ReadAllText(previousPath));

        var isInitial = previous is null;
        var diff = CatalogDiffer.Diff(previous, catalog);
        var markdown = MarkdownRenderer.Render(catalog, diff, isInitial);

        WriteAll(request.OutputPath, markdown);
        if (!string.IsNullOrWhiteSpace(request.SnapshotPath))
            WriteAll(request.SnapshotPath, SnapshotSerializer.Serialize(catalog));

        log.WriteLine($"FluentDocs: wrote {request.OutputPath}");
        if (!string.IsNullOrWhiteSpace(request.SnapshotPath))
            log.WriteLine($"FluentDocs: wrote {request.SnapshotPath}");

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
        File.WriteAllText(fullPath, contents);
    }
}

/// <summary>
/// In-memory result of a documentation generation run.
/// </summary>
public sealed record DocumentationResult(
    SettingsCatalog Catalog,
    CatalogDiff Diff,
    string Markdown,
    bool IsInitial);
