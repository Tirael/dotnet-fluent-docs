namespace FluentDocs.Tests;

public sealed class CatalogDifferTests
{
    [Fact]
    public void Given_modified_previous_catalog_When_diff_is_computed_Then_added_removed_properties_and_rule_ids_are_reported()
    {
        // Arrange
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

        // Act
        var diff = CatalogDiffer.Diff(previous, current);

        // Assert
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
    public void Given_identical_catalogs_When_diff_is_computed_Then_there_are_no_changes()
    {
        // Arrange
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();
        var clone = SnapshotSerializer.Deserialize(SnapshotSerializer.Serialize(catalog));

        // Act
        var diff = CatalogDiffer.Diff(clone, catalog);

        // Assert
        diff.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void Given_null_previous_catalog_When_diff_is_computed_Then_there_are_no_changes()
    {
        // Arrange
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();

        // Act
        var diff = CatalogDiffer.Diff(null, catalog);

        // Assert
        diff.HasChanges.Should().BeFalse();
    }
}
