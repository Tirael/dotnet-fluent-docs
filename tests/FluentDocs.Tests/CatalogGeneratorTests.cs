using FluentDocs.Analysis;
using FluentDocs.Tests.Fixtures;

namespace FluentDocs.Tests;

public sealed class CatalogGeneratorTests
{
    [Fact]
    public void Generate_merges_xml_comments_and_fluent_validation_rules()
    {
        var catalog = GenerateSampleCatalog();
        var mail = Assert.Single(catalog.Types, t => t.Name == "SampleMailOptions");

        Assert.Equal("Mail", mail.ConfigurationPath);
        Assert.Equal("SMTP-like settings used as a generator fixture.", mail.Summary);

        var host = Assert.Single(mail.Properties, p => p.Path == "Host");
        Assert.Equal("string", host.ClrType);
        Assert.Equal("\"localhost\"", host.DefaultValue);
        Assert.Equal("SMTP host name.", host.Summary);
        Assert.Contains(host.Rules, r => r.Id == "NotEmpty");
        Assert.Contains(host.Rules, r => r.Id == "MaximumLength:255");
        Assert.Contains(host.Rules, r => r.Message == "SMTP host is required and must be at most 255 characters.");

        var port = Assert.Single(mail.Properties, p => p.Path == "Port");
        Assert.Contains(port.Rules, r => r.Id == "InclusiveBetween:1-65535");
        Assert.Contains(port.Rules, r => r.Id == "GreaterThanOrEqual:465" && r.HasCondition);

        var retry = Assert.Single(mail.Properties, p => p.Path == "Retry.MaxAttempts");
        Assert.Equal("Maximum attempts.", retry.Summary);
        Assert.Equal("3", retry.DefaultValue);
        Assert.Contains(retry.Rules, r => r.Id == "InclusiveBetween:1-10");

        var email = Assert.Single(mail.Properties, p => p.Path == "Recipients[].Email");
        Assert.Contains(email.Rules, r => r.Id == "EmailAddress");
        Assert.Contains(email.Rules, r => r.Id == "NotEmpty");
    }

    [Fact]
    public void Generate_includes_attributed_storage_settings()
    {
        var catalog = GenerateSampleCatalog();
        var storage = Assert.Single(catalog.Types, t => t.Name == "SampleStorageOptions");
        Assert.Equal("Storage", storage.ConfigurationPath);

        var root = Assert.Single(storage.Properties, p => p.Path == "RootPath");
        Assert.Contains(root.Rules, r => r.Id == "NotEmpty");
        Assert.Contains(root.Rules, r => r.Id.StartsWith("Matches:", StringComparison.Ordinal));

        var size = Assert.Single(storage.Properties, p => p.Path == "MaxFileSizeBytes");
        Assert.Contains(size.Rules, r => r.Id == "InclusiveBetween:1-10000000");
    }

    [Fact]
    public void Generate_does_not_promote_nested_validators_to_top_level_types()
    {
        var catalog = GenerateSampleCatalog();
        Assert.DoesNotContain(catalog.Types, t => t.Name == "SampleRetryOptions");
        Assert.DoesNotContain(catalog.Types, t => t.Name == "SampleRecipientOptions");
    }

    internal static SettingsCatalog GenerateSampleCatalog()
    {
        var assembly = typeof(SampleMailOptions).Assembly;
        var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");
        return CatalogGenerator.Generate(assembly, xmlPath);
    }
}
