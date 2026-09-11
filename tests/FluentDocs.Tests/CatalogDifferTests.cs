using FluentDocs.Tests.Fixtures;

namespace FluentDocs.Tests;

public sealed class CatalogDifferTests
{
    [Fact]
    public void Diff_reports_added_and_removed_properties_and_rule_id_changes()
    {
        // Дано
        var current = CatalogGeneratorTests.GenerateSampleCatalog();
        var previous = SnapshotSerializer.Deserialize(SnapshotSerializer.Serialize(current));
        var mail = previous.Types.Single(t => t.Name == "SampleMailOptions");
        var host = mail.Properties.Single(p => p.Path == "Host");
        host.Rules.RemoveAll(r => r.Id == "MaximumLength:255");
        host.Rules.Add(new SettingsRuleDocument
        {
            Id = "MaximumLength:50",
            Description = "Максимальная длина: 50."
        });
        mail.Properties.RemoveAll(p => p.Path == "From");
        mail.Properties.Add(new SettingsPropertyDocument
        {
            Path = "ReplyTo",
            ClrType = "string",
            Summary = "Адрес для ответа."
        });
        previous.Types.RemoveAll(t => t.Name == "SampleStorageOptions");

        // Когда
        var diff = CatalogDiffer.Diff(previous, current);

        // Тогда
        diff.HasChanges.Should().BeTrue();
        diff.AddedTypes.Should().Contain(t => t.Name == "SampleStorageOptions");

        var mailDiff = diff.ChangedTypes.Should().ContainSingle(t => t.FullName.Contains("SampleMailOptions", StringComparison.Ordinal)).Subject;
        mailDiff.AddedProperties.Should().Contain(p => p.Path == "From");
        mailDiff.RemovedProperties.Should().Contain(p => p.Path == "ReplyTo");

        var hostDiff = mailDiff.ChangedProperties.Should().ContainSingle(p => p.Path == "Host").Subject;
        hostDiff.AddedRules.Should().Contain(r => r.Id == "MaximumLength:255");
        hostDiff.RemovedRules.Should().Contain(r => r.Id == "MaximumLength:50");
        hostDiff.AddedRules.Should().NotContain(r => r.Id == "NotEmpty");
        hostDiff.RemovedRules.Should().NotContain(r => r.Id == "NotEmpty");
    }

    [Fact]
    public void Diff_is_empty_when_catalogs_match()
    {
        // Дано
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();
        var clone = SnapshotSerializer.Deserialize(SnapshotSerializer.Serialize(catalog));

        // Когда
        var diff = CatalogDiffer.Diff(clone, catalog);

        // Тогда
        diff.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void Diff_null_previous_is_empty()
    {
        // Дано
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();

        // Когда
        var diff = CatalogDiffer.Diff(null, catalog);

        // Тогда
        diff.HasChanges.Should().BeFalse();
    }
}
