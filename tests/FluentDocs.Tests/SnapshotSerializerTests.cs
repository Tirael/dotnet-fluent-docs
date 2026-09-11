namespace FluentDocs.Tests;

public sealed class SnapshotSerializerTests
{
    [Fact]
    public void Given_catalog_with_cyrillic_When_serialized_Then_json_keeps_readable_russian()
    {
        // Arrange
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();

        // Act
        var json = SnapshotSerializer.Serialize(catalog);

        // Assert
        json.Should().Contain("SMTP-настройки для фикстуры генератора.");
        json.Should().Contain("Не должно быть пустым.");
        json.Should().NotContain("\\u041");
    }

    [Fact]
    public void Given_serialized_cyrillic_json_When_deserialized_Then_summaries_are_preserved()
    {
        // Arrange
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();
        var json = SnapshotSerializer.Serialize(catalog);

        // Act
        var restored = SnapshotSerializer.Deserialize(json);

        // Assert
        restored.Types.Should().ContainSingle(t => t.Name == "SampleMailOptions")
            .Subject.Summary.Should().Be("SMTP-настройки для фикстуры генератора.");
    }

    [Fact]
    public void Given_catalog_with_cyrillic_When_written_to_file_Then_utf8_bom_is_used_and_text_is_unescaped()
    {
        // Arrange
        var catalog = CatalogGeneratorTests.GenerateSampleCatalog();
        var path = Path.Combine(Path.GetTempPath(), $"fluentdocs-{Guid.NewGuid():N}.json");

        try
        {
            // Act
            SnapshotSerializer.WriteToFile(path, catalog);
            var bytes = File.ReadAllBytes(path);
            var text = System.Text.Encoding.UTF8.GetString(bytes);

            // Assert
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
