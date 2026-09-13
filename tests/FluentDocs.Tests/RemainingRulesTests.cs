namespace FluentDocs.Tests;

public sealed class RemainingRulesTests
{
    [Fact]
    public void Given_remaining_fluentvalidation_rules_When_catalog_is_generated_Then_all_built_in_constraints_are_documented()
    {
        // Arrange
        // Act
        var catalog = TestCompilations.GenerateFromSources(
            "tests/FluentDocs.Tests/Fixtures/RemainingRulesOptions.cs");

        // Assert
        catalog.Warnings.Should().BeEmpty();
        var options = catalog.Types.Should().ContainSingle(t => t.Name == "SampleRemainingRulesOptions").Subject;
        options.ConfigurationPath.Should().Be("Remaining");

        var nickname = options.Properties.Should().ContainSingle(p => p.Path == "Nickname").Subject;
        nickname.Rules.Should().Contain(r => r.Id == "Null" && r.HasCondition);

        var deprecated = options.Properties.Should().ContainSingle(p => p.Path == "Deprecated").Subject;
        deprecated.Rules.Should().Contain(r => r.Id == "Empty");

        var password = options.Properties.Should().ContainSingle(p => p.Path == "Password").Subject;
        password.Rules.Should().Contain(r => r.Id == "NotEqual:password");
        password.Rules.Should().Contain(r => r.Id == "Equal:PasswordConfirmation");

        var status = options.Properties.Should().ContainSingle(p => p.Path == "Status").Subject;
        status.Rules.Should().Contain(r => r.Id == "Enum");

        var statusName = options.Properties.Should().ContainSingle(p => p.Path == "StatusName").Subject;
        statusName.Rules.Should().Contain(r => r.Id == "IsEnumName:SampleStatus:ignoreCase");
        statusName.Rules.Should().Contain(r => r.Description.Contains("без учёта регистра", StringComparison.Ordinal));

        var amount = options.Properties.Should().ContainSingle(p => p.Path == "Amount").Subject;
        amount.Rules.Should().Contain(r => r.Id == "PrecisionScale:4,2,ignoreTrailingZeros");
        amount.Rules.Should().Contain(r => r.Id == "ExclusiveBetween:0-100");

        var card = options.Properties.Should().ContainSingle(p => p.Path == "CardNumber").Subject;
        card.Rules.Should().Contain(r => r.Id == "CreditCard");
        card.Rules.Should().Contain(r => r.Id == "Must" && r.Description.Contains("StartsWith", StringComparison.Ordinal));
        card.Rules.Should().Contain(r => r.Id == "Custom");

        var tags = options.Properties.Should().ContainSingle(p => p.Path == "Tags[]").Subject;
        tags.Rules.Should().Contain(r => r.Id == "NotEmpty");
        tags.Rules.Should().Contain(r => r.Id == "MaximumLength:16");
        tags.Rules.Should().Contain(r => r.Id.StartsWith("Matches:", StringComparison.Ordinal));

        var code = options.Properties.Should().ContainSingle(p => p.Path == "Code").Subject;
        code.Rules.Should().Contain(r => r.Id == "Validator:SamplePrefixValidator");

        var email = options.Properties.Should().ContainSingle(p => p.Path == "Contact.Email").Subject;
        email.Rules.Should().Contain(r => r.Id == "EmailAddress");
        email.Rules.Should().Contain(r => r.Id == "NotEmpty");

        var phone = options.Properties.Should().ContainSingle(p => p.Path == "Contact.Phone").Subject;
        phone.Rules.Should().Contain(r => r.Id.StartsWith("Matches:", StringComparison.Ordinal));
    }
}
