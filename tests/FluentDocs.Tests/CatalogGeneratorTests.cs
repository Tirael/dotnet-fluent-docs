using FluentDocs.Tests.Fixtures;

namespace FluentDocs.Tests;

public sealed class CatalogGeneratorTests
{
    [Fact]
    public void Generate_merges_xml_comments_and_fluent_validation_rules()
    {
        // Дано
        var assembly = typeof(SampleMailOptions).Assembly;
        var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");

        // Когда
        var catalog = CatalogGenerator.Generate(assembly, xmlPath);

        // Тогда
        var mail = catalog.Types.Should().ContainSingle(t => t.Name == "SampleMailOptions").Subject;
        mail.ConfigurationPath.Should().Be("Mail");
        mail.Summary.Should().Be("SMTP-настройки для фикстуры генератора.");

        var host = mail.Properties.Should().ContainSingle(p => p.Path == "Host").Subject;
        host.ClrType.Should().Be("string");
        host.DefaultValue.Should().Be("\"localhost\"");
        host.Summary.Should().Be("Имя SMTP-хоста.");
        host.Rules.Should().Contain(r => r.Id == "NotEmpty");
        host.Rules.Should().Contain(r => r.Id == "MaximumLength:255");
        host.Rules.Should().Contain(r => r.Message == "SMTP-хост обязателен и не длиннее 255 символов.");

        var port = mail.Properties.Should().ContainSingle(p => p.Path == "Port").Subject;
        port.Rules.Should().Contain(r => r.Id == "InclusiveBetween:1-65535");
        port.Rules.Should().Contain(r => r.Id == "GreaterThanOrEqual:465" && r.HasCondition);

        var retry = mail.Properties.Should().ContainSingle(p => p.Path == "Retry.MaxAttempts").Subject;
        retry.Summary.Should().Be("Максимальное число попыток.");
        retry.DefaultValue.Should().Be("3");
        retry.Rules.Should().Contain(r => r.Id == "InclusiveBetween:1-10");

        var email = mail.Properties.Should().ContainSingle(p => p.Path == "Recipients[].Email").Subject;
        email.Rules.Should().Contain(r => r.Id == "EmailAddress");
        email.Rules.Should().Contain(r => r.Id == "NotEmpty");
    }

    [Fact]
    public void Generate_includes_attributed_storage_settings()
    {
        // Дано
        var assembly = typeof(SampleStorageOptions).Assembly;
        var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");

        // Когда
        var catalog = CatalogGenerator.Generate(assembly, xmlPath);

        // Тогда
        var storage = catalog.Types.Should().ContainSingle(t => t.Name == "SampleStorageOptions").Subject;
        storage.ConfigurationPath.Should().Be("Storage");

        var root = storage.Properties.Should().ContainSingle(p => p.Path == "RootPath").Subject;
        root.Rules.Should().Contain(r => r.Id == "NotEmpty");
        root.Rules.Should().Contain(r => r.Id.StartsWith("Matches:", StringComparison.Ordinal));

        var size = storage.Properties.Should().ContainSingle(p => p.Path == "MaxFileSizeBytes").Subject;
        size.Rules.Should().Contain(r => r.Id == "InclusiveBetween:1-10000000");
    }

    [Fact]
    public void Generate_does_not_promote_nested_validators_to_top_level_types()
    {
        // Дано
        var assembly = typeof(SampleMailOptions).Assembly;
        var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");

        // Когда
        var catalog = CatalogGenerator.Generate(assembly, xmlPath);

        // Тогда
        catalog.Types.Should().NotContain(t => t.Name == "SampleRetryOptions");
        catalog.Types.Should().NotContain(t => t.Name == "SampleRecipientOptions");
    }

    internal static SettingsCatalog GenerateSampleCatalog()
    {
        var assembly = typeof(SampleMailOptions).Assembly;
        var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");
        return CatalogGenerator.Generate(assembly, xmlPath);
    }
}
