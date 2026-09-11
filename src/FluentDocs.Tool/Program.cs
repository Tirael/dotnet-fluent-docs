using FluentDocs;

namespace FluentDocs.Tool;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = CliOptions.Parse(args);
            if (options.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            DocumentationPipeline.Run(new DocumentationRequest
            {
                SourceFiles = options.SourceFiles,
                References = options.References,
                XmlDocumentationPath = options.XmlPath,
                OutputPath = options.OutputPath,
                SnapshotPath = options.SnapshotPath,
                PreviousSnapshotPath = options.PreviousSnapshotPath
            }, Console.Out);

            return 0;
        }
        catch (CliException ex)
        {
            Console.Error.WriteLine(ex.Message);
            PrintHelp();
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FluentDocs завершился с ошибкой: {ex}");
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            FluentDocs — генерация описания настроек по исходникам валидаторов FluentValidation (Roslyn).

            Параметры:
              --sources-list <path>         Файл со списком исходников (по одному пути на строку)
              --references-list <path>      Файл со списком сборок для семантической модели
              --source <path>               Отдельный исходный файл (можно повторять)
              --reference <path>            Отдельная ссылка на сборку (можно повторять)
              --xml <path>                  XML-файл документации компилятора
              --output <path>               Путь к Markdown-файлу (обязательный)
              --snapshot <path>             Путь к JSON-снимку (если файл есть, он читается как предыдущий)
              --previous-snapshot <path>    Явный предыдущий JSON-снимок
              --help                        Показать эту справку
            """);
    }
}

internal sealed class CliOptions
{
    public IReadOnlyList<string> SourceFiles { get; init; } = [];
    public IReadOnlyList<string> References { get; init; } = [];
    public string? XmlPath { get; init; }
    public required string OutputPath { get; init; }
    public string? SnapshotPath { get; init; }
    public string? PreviousSnapshotPath { get; init; }
    public bool ShowHelp { get; init; }

    public static CliOptions Parse(string[] args)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-h" or "--help")
            {
                map["help"] = ["true"];
                continue;
            }

            if (!arg.StartsWith("--", StringComparison.Ordinal))
                throw new CliException($"Неожиданный аргумент '{arg}'.");

            var key = arg[2..];
            string? value = null;
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                value = args[++i];

            if (!map.TryGetValue(key, out var list))
            {
                list = [];
                map[key] = list;
            }

            if (value is not null)
                list.Add(value);
        }

        if (map.ContainsKey("help"))
        {
            return new CliOptions
            {
                OutputPath = "",
                ShowHelp = true
            };
        }

        if (!map.TryGetValue("output", out var output) || output.Count == 0 || string.IsNullOrWhiteSpace(output[0]))
            throw new CliException("Отсутствует обязательный аргумент --output.");

        var sources = new List<string>();
        var references = new List<string>();

        if (map.TryGetValue("sources-list", out var sourcesList))
        {
            foreach (var file in sourcesList)
                sources.AddRange(ReadPathList(file));
        }

        if (map.TryGetValue("references-list", out var refsList))
        {
            foreach (var file in refsList)
                references.AddRange(ReadPathList(file));
        }

        if (map.TryGetValue("source", out var sourceArgs))
            sources.AddRange(sourceArgs);
        if (map.TryGetValue("reference", out var referenceArgs))
            references.AddRange(referenceArgs);

        if (sources.Count == 0)
            throw new CliException("Нужно указать --sources-list или хотя бы один --source.");

        map.TryGetValue("xml", out var xml);
        map.TryGetValue("snapshot", out var snapshot);
        map.TryGetValue("previous-snapshot", out var previous);

        return new CliOptions
        {
            SourceFiles = sources,
            References = references,
            XmlPath = xml?.FirstOrDefault(),
            OutputPath = output[0],
            SnapshotPath = snapshot?.FirstOrDefault(),
            PreviousSnapshotPath = previous?.FirstOrDefault()
        };
    }

    private static IEnumerable<string> ReadPathList(string? listPath)
    {
        if (string.IsNullOrWhiteSpace(listPath) || !File.Exists(listPath))
            return [];

        return File.ReadAllLines(listPath)
            .Select(static line => line.Trim())
            .Where(static line => line.Length > 0);
    }
}

internal sealed class CliException(string message) : Exception(message);
