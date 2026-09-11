using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace FluentDocs.Analysis;

internal sealed class XmlMemberDocs
{
    public string? Summary { get; init; }
    public string? Remarks { get; init; }
}

internal sealed class XmlDocumentationReader
{
    private readonly Dictionary<string, XmlMemberDocs> _members;

    private XmlDocumentationReader(Dictionary<string, XmlMemberDocs> members)
    {
        _members = members;
    }

    public static XmlDocumentationReader Empty { get; } = new(new Dictionary<string, XmlMemberDocs>(StringComparer.Ordinal));

    public static XmlDocumentationReader Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return Empty;

        var document = XDocument.Load(path);
        var members = new Dictionary<string, XmlMemberDocs>(StringComparer.Ordinal);
        foreach (var member in document.Descendants("member"))
        {
            var name = (string?)member.Attribute("name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            members[name] = new XmlMemberDocs
            {
                Summary = Normalize(member.Element("summary")),
                Remarks = Normalize(member.Element("remarks"))
            };
        }

        return new XmlDocumentationReader(members);
    }

    public XmlMemberDocs? Get(string documentationId)
        => _members.TryGetValue(documentationId, out var docs) ? docs : null;

    /// <summary>
    /// Берёт комментарий из XML-файла компилятора, иначе из исходника через Roslyn.
    /// </summary>
    public XmlMemberDocs? Get(ISymbol symbol)
    {
        var id = symbol.GetDocumentationCommentId();
        var fromFile = id is null ? null : Get(id);
        var fromSource = ParseSymbolComment(symbol);
        if (fromFile is null)
            return fromSource;
        if (fromSource is null)
            return fromFile;

        return new XmlMemberDocs
        {
            Summary = fromFile.Summary ?? fromSource.Summary,
            Remarks = fromFile.Remarks ?? fromSource.Remarks
        };
    }

    private static XmlMemberDocs? ParseSymbolComment(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml(expandIncludes: true);
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        try
        {
            var element = XElement.Parse(xml);
            return new XmlMemberDocs
            {
                Summary = Normalize(element.Element("summary") ?? element.Descendants("summary").FirstOrDefault()),
                Remarks = Normalize(element.Element("remarks") ?? element.Descendants("remarks").FirstOrDefault())
            };
        }
        catch (System.Xml.XmlException)
        {
            return new XmlMemberDocs { Summary = Normalize(xml) };
        }
    }

    internal static string? Normalize(XElement? element)
    {
        if (element is null)
            return null;

        var builder = new StringBuilder();
        AppendNode(element, builder);
        return Normalize(builder.ToString());
    }

    internal static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var text = Regex.Replace(value, """<see\s+cref="(?:[A-Z]:)?([^"]+)"\s*/>""", "$1", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", " ");
        text = WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string FormatCref(string cref)
    {
        var id = cref.Length >= 2 && cref[1] == ':' ? cref[2..] : cref;
        var lastDot = id.LastIndexOf('.');
        return lastDot >= 0 ? id[(lastDot + 1)..] : id;
    }

    private static void AppendNode(XNode node, StringBuilder builder)
    {
        switch (node)
        {
            case XText text:
                builder.Append(text.Value);
                break;
            case XElement { Name.LocalName: "see" } see:
                {
                    var cref = (string?)see.Attribute("cref");
                    if (!string.IsNullOrWhiteSpace(cref))
                        builder.Append(FormatCref(cref));
                    else if (!string.IsNullOrWhiteSpace(see.Value))
                        builder.Append(see.Value);

                    break;
                }
            case XElement element:
                foreach (var child in element.Nodes())
                    AppendNode(child, builder);
                break;
        }
    }
}
