namespace FluentDocs.Tests;

public sealed class MarkdownRendererTests
{
    [Fact]
    public void Given_sample_catalog_When_markdown_is_rendered_Then_it_matches_committed_snapshot()
    {
        // Arrange
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "Snapshots", "sample-catalog.md");
        Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);

        // Act
        var markdown = MarkdownRenderer.Render(catalog, new CatalogDiff(), isInitial: true);
        if (!File.Exists(expectedPath))
            File.WriteAllText(expectedPath, markdown);

        // Assert
        var expected = File.ReadAllText(expectedPath);
        Normalize(markdown).Should().Be(Normalize(expected));
    }

    [Fact]
    public void Given_changed_rule_ids_When_markdown_is_rendered_Then_changelog_lists_added_and_removed_constraints()
    {
        // Arrange
        var current = CatalogGeneratorTests.GenerateSampleCatalog();
        var previous = SnapshotSerializer.Deserialize(SnapshotSerializer.Serialize(current));
        var host = previous.Types.Single(t => t.Name == "SampleMailOptions").Properties.Single(p => p.Path == "Host");
        host.Rules.RemoveAll(r => r.Id == "MaximumLength:255");
        host.Rules.Add(new SettingsRuleDocument { Id = "MaximumLength:50", Description = "Максимальная длина: 50." });

        // Act
        var diff = CatalogDiffer.Diff(previous, current);
        var markdown = MarkdownRenderer.Render(current, diff, isInitial: false);

        // Assert
        markdown.Should().Contain("## Журнал изменений");
        markdown.Should().Contain("Добавлено ограничение `MaximumLength:255`");
        markdown.Should().Contain("Удалено ограничение `MaximumLength:50`");
        markdown.Should().Contain("#### Host");
        markdown.Should().Contain("Имя SMTP-хоста.");
    }

    private static string Normalize(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal);
}
