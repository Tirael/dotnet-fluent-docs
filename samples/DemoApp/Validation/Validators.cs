using DemoApp.Options;
using FluentValidation;

namespace DemoApp.Validation;

/// <summary>
/// FluentValidation rules for <see cref="MailOptions"/>.
/// </summary>
public sealed class MailOptionsValidator : AbstractValidator<MailOptions>
{
    public MailOptionsValidator()
    {
        RuleFor(x => x.Host)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("SMTP host is required and must be at most 255 characters.");

        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65_535);

        RuleFor(x => x.From)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("From must be a valid email address.");

        When(x => x.EnableTls, () =>
        {
            RuleFor(x => x.Port)
                .GreaterThanOrEqualTo(465)
                .WithMessage("TLS mail typically uses port 465 or 587.");
        });

        RuleFor(x => x.TimeoutSeconds)
            .InclusiveBetween(1, 300);

        RuleFor(x => x.Retry)
            .SetValidator(new RetryOptionsValidator());

        RuleFor(x => x.Recipients)
            .NotNull();

        RuleForEach(x => x.Recipients)
            .SetValidator(new RecipientOptionsValidator());

        RuleForEach(x => x.AllowedSenderPatterns)
            .NotEmpty()
            .Matches(@"^.*$");
    }
}

/// <summary>
/// FluentValidation rules for <see cref="RetryOptions"/>.
/// </summary>
public sealed class RetryOptionsValidator : AbstractValidator<RetryOptions>
{
    public RetryOptionsValidator()
    {
        RuleFor(x => x.MaxAttempts)
            .InclusiveBetween(1, 10);

        RuleFor(x => x.DelayMilliseconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(60_000);
    }
}

/// <summary>
/// FluentValidation rules for <see cref="RecipientOptions"/>.
/// </summary>
public sealed class RecipientOptionsValidator : AbstractValidator<RecipientOptions>
{
    public RecipientOptionsValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Name)
            .MaximumLength(100)
            .When(x => x.Name is not null);
    }
}

/// <summary>
/// FluentValidation rules for <see cref="StorageOptions"/>.
/// </summary>
public sealed class StorageOptionsValidator : AbstractValidator<StorageOptions>
{
    public StorageOptionsValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .Must(provider => provider is "Local" or "S3")
            .WithMessage("Provider must be Local or S3.");

        RuleFor(x => x.RootPath)
            .NotEmpty()
            .Must(path => path.StartsWith('/'))
            .WithMessage("RootPath must be an absolute Unix path.");

        RuleFor(x => x.MaxFileSizeBytes)
            .InclusiveBetween(1, 100 * 1024 * 1024);

        When(x => x.Provider == "S3", () =>
        {
            RuleFor(x => x.BucketName)
                .NotEmpty()
                .Matches("^[a-z0-9.-]{3,63}$")
                .WithMessage("BucketName must be a valid S3 bucket name.");
        });
    }
}
