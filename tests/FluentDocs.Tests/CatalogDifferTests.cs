using FluentDocs.Diff;
using FluentDocs.Rendering;
using FluentDocs.Tests.Fixtures;

namespace FluentDocs.Tests;

public sealed class CatalogDifferTests
{
    [Fact]
    public void Diff_reports_added_and_removed_properties_and_rule_id_changes()
    {
        var current = CatalogGeneratorTests.GenerateSampleCatalog();
        var previous = SnapshotSerializer.Deserialize(SnapshotSerializer.Serialize(current));

        var mail = previous.Types.Single(t => t.Name == "SampleMailOptions");
        var host = mail.Properties.Single(p => p.Path == "Host");
        host.Rules.RemoveAll(r => r.Id == "MaximumLength:255");
        host.Rules.Add(new SettingsRuleDocument
        {
            Id = "MaximumLength:50",
            Description = "Maximum length is 50."
        });

        mail.Properties.RemoveAll(p => p.Path == "From");
        mail.Properties.Add(new SettingsPropertyDocument
        {
            Path = "ReplyTo",
            ClrType = "string",
            Summary = "Reply-to address."
        });

        previous.Types.RemoveAll(t => t.Name == "SampleStorageOptions");

        var diff = CatalogDiffer.Diff(previous, current);

        Assert.True(diff.HasChanges);
        Assert.Contains(diff.AddedTypes, t => t.Name == "SampleStorageOptions");

        var mailDiff = Assert.Single(diff.ChangedTypes, t => t.FullName.Contains("SampleMailOptions", StringComparison.Ordinal));
        Assert.Contains(mailDiff.AddedProperties, p => p.Path == "From");
        Assert.Contains(mailDiff.RemovedProperties, p => p.Path == "ReplyTo");

        var hostDiff = Assert.Single(mailDiff.ChangedProperties, p => p.Path == "Host");
        Assert.Contains(hostDiff.AddedRules, r => r.Id == "MaximumLength:255");
        Assert.Contains(hostDiff.RemovedRules, r => r.Id == "MaximumLength:50");
        Assert.DoesNotContain(hostDiff.AddedRules, r => r.Id == "NotEmpty");
        Assert.DoesNotContain(hostDiff.RemovedRules, r => r.Id == "NotEmpty");
    }

    [Fact]
    public void Diff_is_empty_when_catalogs_match()
    {
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();
        var clone = SnapshotSerializer.Deserialize(SnapshotSerializer.Serialize(catalog));
        var diff = CatalogDiffer.Diff(clone, catalog);
        Assert.False(diff.HasChanges);
    }

    [Fact]
    public void Diff_null_previous_is_empty()
    {
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();
        var diff = CatalogDiffer.Diff(null, catalog);
        Assert.False(diff.HasChanges);
    }
}
