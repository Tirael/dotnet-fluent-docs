using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FluentDocs.Analysis;

/// <summary>
/// Собирает <see cref="CSharpCompilation"/> из списков исходников и ссылок.
/// </summary>
internal static class CompilationFactory
{
    public static CSharpCompilation Create(
        IEnumerable<string> sourceFiles,
        IEnumerable<string> metadataReferences,
        string assemblyName = "FluentDocs.Analyzed")
    {
        ArgumentNullException.ThrowIfNull(sourceFiles);
        ArgumentNullException.ThrowIfNull(metadataReferences);

        var parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.Preview,
            documentationMode: DocumentationMode.Parse);

        var trees = new List<SyntaxTree>();
        foreach (var file in sourceFiles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(file))
                continue;

            var fullPath = Path.GetFullPath(file);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Исходный файл '{fullPath}' не найден.", fullPath);

            var text = File.ReadAllText(fullPath);
            trees.Add(CSharpSyntaxTree.ParseText(text, parseOptions, path: fullPath));
        }

        if (trees.Count == 0)
            throw new ArgumentException("Нужно указать хотя бы один исходный файл.", nameof(sourceFiles));

        var references = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in metadataReferences)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath) || !seen.Add(fullPath))
                continue;

            references.Add(MetadataReference.CreateFromFile(fullPath));
        }

        var options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable,
            usings:
            [
                "System",
                "System.Collections.Generic",
                "System.IO",
                "System.Linq",
                "System.Net.Http",
                "System.Threading",
                "System.Threading.Tasks"
            ]);

        return CSharpCompilation.Create(assemblyName, trees, references, options);
    }

    public static IReadOnlyList<string> ReadPathList(string? listPath)
    {
        if (string.IsNullOrWhiteSpace(listPath) || !File.Exists(listPath))
            return [];

        return File.ReadAllLines(listPath)
            .Select(static line => line.Trim())
            .Where(static line => line.Length > 0)
            .ToList();
    }
}
