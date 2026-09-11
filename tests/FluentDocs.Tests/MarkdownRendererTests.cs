namespace FluentDocs.Tests;

public sealed class MarkdownRendererTests
{
    [Fact]
    public void Render_matches_committed_snapshot()
    {
        // Дано
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "Snapshots", "sample-catalog.md");
        Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);

        // Когда
        var markdown = MarkdownRenderer.Render(catalog, new CatalogDiff(), isInitial: true);
        if (!File.Exists(expectedPath))
            File.WriteAllText(expectedPath, markdown);

        // Тогда
        var expected = File.ReadAllText(expectedPath);
        Normalize(markdown).Should().Be(Normalize(expected));
    }

    [Fact]
    public void Render_includes_changelog_for_rule_changes()
    {
        // Дано
        var current = CatalogGeneratorTests.GenerateSampleCatalog();
        var previous = SnapshotSerializer.Deserialize(SnapshotSerializer.Serialize(current));
        var host = previous.Types.Single(t => t.Name == "SampleMailOptions").Properties.Single(p => p.Path == "Host");
        host.Rules.RemoveAll(r => r.Id == "MaximumLength:255");
        host.Rules.Add(new SettingsRuleDocument { Id = "MaximumLength:50", Description = "Максимальная длина: 50." });

        // Когда
        var diff = CatalogDiffer.Diff(previous, current);
        var markdown = MarkdownRenderer.Render(current, diff, isInitial: false);

        // Тогда
        markdown.Should().Contain("## Журнал изменений");
        markdown.Should().Contain("Добавлено ограничение `MaximumLength:255`");
        markdown.Should().Contain("Удалено ограничение `MaximumLength:50`");
        markdown.Should().Contain("#### Host");
        markdown.Should().Contain("Имя SMTP-хоста.");
    }

    private static string Normalize(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal);
}
