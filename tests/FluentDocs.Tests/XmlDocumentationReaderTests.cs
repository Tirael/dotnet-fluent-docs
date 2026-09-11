using FluentDocs.Analysis;

namespace FluentDocs.Tests;

public sealed class XmlDocumentationReaderTests
{
    [Fact]
    public void Load_reads_summary_and_strips_see_tags()
    {
        // Дано
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
            // Когда
            var reader = XmlDocumentationReader.Load(path);
            var docs = reader.Get("T:Demo.MailOptions");

            // Тогда
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
    public void Normalize_collapses_whitespace()
    {
        // Дано
        var raw = """
              Первая строка.
              Вторая строка.
            """;

        // Когда
        var text = XmlDocumentationReader.Normalize(raw);

        // Тогда
        text.Should().Be("Первая строка. Вторая строка.");
    }
}
