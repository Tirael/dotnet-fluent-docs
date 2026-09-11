using FluentDocs.Analysis;

namespace FluentDocs.Tests;

public sealed class XmlDocumentationReaderTests
{
    [Fact]
    public void Given_xml_docs_with_see_tags_When_member_is_loaded_Then_summary_is_normalized()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), $"fluentdocs-{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, """
            <?xml version="1.0"?>
            <doc>
              <members>
                <member name="T:Demo.MailOptions">
                  <summary>
                    Настройки SMTP. См. <see cref="T:Demo.RetryOptions"/>.
                  </summary>
                  <remarks>Привязка к Mail.</remarks>
                </member>
              </members>
            </doc>
            """);

        try
        {
            // Act
            var reader = XmlDocumentationReader.Load(path);
            var docs = reader.Get("T:Demo.MailOptions");

            // Assert
            docs.Should().NotBeNull();
            docs!.Summary.Should().Be("Настройки SMTP. См. RetryOptions.");
            docs.Remarks.Should().Be("Привязка к Mail.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Given_multiline_xml_text_When_normalized_Then_whitespace_is_collapsed()
    {
        // Arrange
        var raw = """
              Первая строка.
              Вторая строка.
            """;

        // Act
        var text = XmlDocumentationReader.Normalize(raw);

        // Assert
        text.Should().Be("Первая строка. Вторая строка.");
    }
}
