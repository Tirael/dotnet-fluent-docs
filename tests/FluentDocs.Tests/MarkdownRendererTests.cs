using FluentDocs.Diff;
using FluentDocs.Rendering;

namespace FluentDocs.Tests;

public sealed class MarkdownRendererTests
{
    [Fact]
    public void Render_matches_committed_snapshot()
    {
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();
        var markdown = MarkdownRenderer.Render(catalog, new CatalogDiff(), isInitial: true);
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "Snapshots", "sample-catalog.md");

        Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
        if (!File.Exists(expectedPath))
        {
            File.WriteAllText(expectedPath, markdown);
        }

        var expected = File.ReadAllText(expectedPath);
        Assert.Equal(Normalize(expected), Normalize(markdown));
    }

    [Fact]
    public void Render_includes_changelog_for_rule_changes()
    {
        var current = CatalogGeneratorTests.GenerateSampleCatalog();
        var previous = SnapshotSerializer.Deserialize(SnapshotSerializer.Serialize(current));
        var host = previous.Types.Single(t => t.Name == "SampleMailOptions").Properties.Single(p => p.Path == "Host");
        host.Rules.RemoveAll(r => r.Id == "MaximumLength:255");
        host.Rules.Add(new SettingsRuleDocument { Id = "MaximumLength:50", Description = "Maximum length is 50." });

        var diff = CatalogDiffer.Diff(previous, current);
        var markdown = MarkdownRenderer.Render(current, diff, isInitial: false);

        Assert.Contains("## Changelog", markdown, StringComparison.Ordinal);
        Assert.Contains("Added constraint `MaximumLength:255`", markdown, StringComparison.Ordinal);
        Assert.Contains("Removed constraint `MaximumLength:50`", markdown, StringComparison.Ordinal);
        Assert.Contains("#### Host", markdown, StringComparison.Ordinal);
        Assert.Contains("SMTP host name.", markdown, StringComparison.Ordinal);
    }

    private static string Normalize(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal);
}
