namespace FluentDocs.Tests;

public sealed class SnapshotSerializerTests
{
    [Fact]
    public void Serialize_keeps_cyrillic_readable()
    {
        // Дано
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();

        // Когда
        var json = SnapshotSerializer.Serialize(catalog);

        // Тогда
        json.Should().Contain("SMTP-настройки для фикстуры генератора.");
        json.Should().Contain("Не должно быть пустым.");
        json.Should().NotContain("\\u041");
    }

    [Fact]
    public void Deserialize_reads_readable_cyrillic_snapshot()
    {
        // Дано
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();
        var json = SnapshotSerializer.Serialize(catalog);

        // Когда
        var restored = SnapshotSerializer.Deserialize(json);

        // Тогда
        restored.Types.Should().ContainSingle(t => t.Name == "SampleMailOptions")
            .Subject.Summary.Should().Be("SMTP-настройки для фикстуры генератора.");
    }
}
