namespace FluentDocs.Tests;

public sealed class CatalogGeneratorTests
{
    [Fact]
    public void Given_sample_options_with_validators_When_catalog_is_generated_Then_xml_comments_and_rules_are_merged()
    {
        // Arrange
        // Act
        var catalog = GenerateSampleCatalog();

        // Assert
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
    public void Given_settings_docs_attribute_When_catalog_is_generated_Then_storage_settings_are_included()
    {
        // Arrange
        // Act
        var catalog = GenerateSampleCatalog();

        // Assert
        var storage = catalog.Types.Should().ContainSingle(t => t.Name == "SampleStorageOptions").Subject;
        storage.ConfigurationPath.Should().Be("Storage");

        var root = storage.Properties.Should().ContainSingle(p => p.Path == "RootPath").Subject;
        root.Rules.Should().Contain(r => r.Id == "NotEmpty");
        root.Rules.Should().Contain(r => r.Id.StartsWith("Matches:", StringComparison.Ordinal));

        var size = storage.Properties.Should().ContainSingle(p => p.Path == "MaxFileSizeBytes").Subject;
        size.Rules.Should().Contain(r => r.Id == "InclusiveBetween:1-10000000");
    }

    [Fact]
    public void Given_nested_child_validators_When_catalog_is_generated_Then_they_are_not_promoted_to_top_level()
    {
        // Arrange
        // Act
        var catalog = GenerateSampleCatalog();

        // Assert
        catalog.Types.Should().NotContain(t => t.Name == "SampleRetryOptions");
        catalog.Types.Should().NotContain(t => t.Name == "SampleRecipientOptions");
    }

    [Fact]
    public void Given_validator_with_constructor_dependency_When_catalog_is_generated_Then_rules_are_read_from_source()
    {
        // Arrange
        // Act
        var catalog = TestCompilations.GenerateFromSources(
            "tests/FluentDocs.Tests/Fixtures/DiOptions.cs");

        // Assert
        var di = catalog.Types.Should().ContainSingle(t => t.Name == "SampleDiOptions").Subject;
        di.ConfigurationPath.Should().Be("Di");
        var name = di.Properties.Should().ContainSingle(p => p.Path == "ProfileName").Subject;
        name.DefaultValue.Should().Be("\"default\"");
        name.Rules.Should().Contain(r => r.Id == "NotEmpty");
        name.Rules.Should().Contain(r => r.Id == "MaximumLength:32");
        catalog.Warnings.Should().NotContain(w => w.Contains("конструктор без параметров", StringComparison.Ordinal));
    }

    internal static SettingsCatalog GenerateSampleCatalog()
        => TestCompilations.GenerateSampleCatalog();
}
