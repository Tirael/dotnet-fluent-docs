namespace FluentDocs.Tests;

internal static class TestCompilations
{
    public static SettingsCatalog GenerateSampleCatalog()
        => CatalogGenerator.GenerateFromFiles(SampleSources(), MetadataReferences(), SampleXmlPath());

    public static SettingsCatalog GenerateFromSources(params string[] relativeSourcePaths)
        => CatalogGenerator.GenerateFromFiles(ResolveSources(relativeSourcePaths), MetadataReferences(), SampleXmlPath());

    public static IReadOnlyList<string> SampleSources()
        => ResolveSources("tests/FluentDocs.Tests/Fixtures/SampleOptions.cs");

    public static string? SampleXmlPath()
    {
        var xml = Path.ChangeExtension(typeof(Fixtures.SampleMailOptions).Assembly.Location, ".xml");
        return File.Exists(xml) ? xml : null;
    }

    public static IReadOnlyList<string> MetadataReferences()
    {
        var paths = new List<string>();
        var trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (!string.IsNullOrWhiteSpace(trusted))
        {
            paths.AddRange(trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries));
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                paths.Add(assembly.Location);
        }

        return paths
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> ResolveSources(params string[] relativeSourcePaths)
    {
        var root = FindRepoRoot();
        return relativeSourcePaths
            .Select(path => Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)))
            .ToList();
    }

    internal static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "dotnet-features-changelogs.sln"))
                || File.Exists(Path.Combine(directory.FullName, "dotnet-features-changelogs.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Не удалось найти корень репозитория.");
    }
}
