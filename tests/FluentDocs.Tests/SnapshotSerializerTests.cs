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

    [Fact]
    public void WriteToFile_stores_utf8_cyrillic_without_escapes()
    {
        // Дано
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();
        var path = Path.Combine(Path.GetTempPath(), $"fluentdocs-{Guid.NewGuid():N}.json");

        try
        {
            // Когда
            SnapshotSerializer.WriteToFile(path, catalog);
            var bytes = File.ReadAllBytes(path);
            var text = System.Text.Encoding.UTF8.GetString(bytes);

            // Тогда
            bytes.Should().StartWith([0xEF, 0xBB, 0xBF]);
            text.Should().Contain("SMTP-настройки для фикстуры генератора.");
            text.Should().NotContain("\\u041");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
