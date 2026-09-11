using FluentDocs.Analysis;

namespace FluentDocs.Tests;

public sealed class XmlDocumentationReaderTests
{
    [Fact]
    public void Load_reads_summary_and_strips_see_tags()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fluentdocs-{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, """
            <?xml version="1.0"?>
            <doc>
              <members>
                <member name="T:Demo.MailOptions">
                  <summary>
                    SMTP settings. See <see cref="T:Demo.RetryOptions"/>.
                  </summary>
                  <remarks>Bound to Mail.</remarks>
                </member>
              </members>
            </doc>
            """);

        try
        {
            var reader = XmlDocumentationReader.Load(path);
            var docs = reader.Get("T:Demo.MailOptions");
            Assert.NotNull(docs);
            Assert.Equal("SMTP settings. See RetryOptions.", docs.Summary);
            Assert.Equal("Bound to Mail.", docs.Remarks);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Normalize_collapses_whitespace()
    {
        var text = XmlDocumentationReader.Normalize("""
              Line one.
              Line two.
            """);
        Assert.Equal("Line one. Line two.", text);
    }
}
