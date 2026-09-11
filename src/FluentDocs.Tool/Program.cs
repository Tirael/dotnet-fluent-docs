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
                AssemblyPath = options.AssemblyPath,
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
            FluentDocs — генерация описания настроек по валидаторам FluentValidation.

            Параметры:
              --assembly <path>             Собранная сборка приложения (обязательный)
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
    public required string AssemblyPath { get; init; }
    public string? XmlPath { get; init; }
    public required string OutputPath { get; init; }
    public string? SnapshotPath { get; init; }
    public string? PreviousSnapshotPath { get; init; }
    public bool ShowHelp { get; init; }

    public static CliOptions Parse(string[] args)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg is "-h" or "--help")
            {
                map["help"] = "true";
                continue;
            }

            if (!arg.StartsWith("--", StringComparison.Ordinal))
                throw new CliException($"Неожиданный аргумент '{arg}'.");

            var key = arg[2..];
            string? value = null;
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = args[++i];
            }

            map[key] = value;
        }

        if (map.ContainsKey("help"))
        {
            return new CliOptions
            {
                AssemblyPath = "",
                OutputPath = "",
                ShowHelp = true
            };
        }

        if (!map.TryGetValue("assembly", out var assembly) || string.IsNullOrWhiteSpace(assembly))
            throw new CliException("Отсутствует обязательный аргумент --assembly.");
        if (!map.TryGetValue("output", out var output) || string.IsNullOrWhiteSpace(output))
            throw new CliException("Отсутствует обязательный аргумент --output.");

        map.TryGetValue("xml", out var xml);
        map.TryGetValue("snapshot", out var snapshot);
        map.TryGetValue("previous-snapshot", out var previous);

        return new CliOptions
        {
            AssemblyPath = assembly,
            XmlPath = xml,
            OutputPath = output,
            SnapshotPath = snapshot,
            PreviousSnapshotPath = previous
        };
    }
}

internal sealed class CliException(string message) : Exception(message);
