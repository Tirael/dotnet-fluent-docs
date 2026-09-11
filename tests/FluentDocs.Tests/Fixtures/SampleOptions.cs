using FluentDocs;
using FluentValidation;

namespace FluentDocs.Tests.Fixtures;

/// <summary>
/// SMTP-like settings used as a generator fixture.
/// </summary>
[SettingsDocs("Mail")]
public sealed class SampleMailOptions
{
    /// <summary>
    /// SMTP host name.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// SMTP port.
    /// </summary>
    public int Port { get; set; } = 25;

    /// <summary>
    /// Sender address.
    /// </summary>
    public string From { get; set; } = "noreply@localhost";

    /// <summary>
    /// Enables TLS after connect.
    /// </summary>
    public bool EnableTls { get; set; }

    /// <summary>
    /// Nested retry policy.
    /// </summary>
    public SampleRetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Notification recipients.
    /// </summary>
    public List<SampleRecipientOptions> Recipients { get; set; } = [];
}

/// <summary>
/// Retry policy fixture.
/// </summary>
public sealed class SampleRetryOptions
{
    /// <summary>
    /// Maximum attempts.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Delay in milliseconds.
    /// </summary>
    public int DelayMilliseconds { get; set; } = 200;
}

/// <summary>
/// Recipient fixture.
/// </summary>
public sealed class SampleRecipientOptions
{
    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Storage settings fixture.
/// </summary>
[SettingsDocs("Storage")]
public sealed class SampleStorageOptions
{
    /// <summary>
    /// Absolute root path.
    /// </summary>
    public string RootPath { get; set; } = "/data";

    /// <summary>
    /// Maximum file size in bytes.
    /// </summary>
    public int MaxFileSizeBytes { get; set; } = 1024;
}

public sealed class SampleMailOptionsValidator : AbstractValidator<SampleMailOptions>
{
    public SampleMailOptionsValidator()
    {
        RuleFor(x => x.Host)
            .NotEmpty()
            .MaximumLength(255)
            .WithMessage("SMTP host is required and must be at most 255 characters.");

        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65_535);

        When(x => x.EnableTls, () =>
        {
            RuleFor(x => x.Port)
                .GreaterThanOrEqualTo(465);
        });

        RuleFor(x => x.From)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Retry)
            .SetValidator(new SampleRetryOptionsValidator());

        RuleForEach(x => x.Recipients)
            .SetValidator(new SampleRecipientOptionsValidator());
    }
}

public sealed class SampleRetryOptionsValidator : AbstractValidator<SampleRetryOptions>
{
    public SampleRetryOptionsValidator()
    {
        RuleFor(x => x.MaxAttempts).InclusiveBetween(1, 10);
        RuleFor(x => x.DelayMilliseconds).GreaterThan(0);
    }
}

public sealed class SampleRecipientOptionsValidator : AbstractValidator<SampleRecipientOptions>
{
    public SampleRecipientOptionsValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Name).MaximumLength(100);
    }
}

public sealed class SampleStorageOptionsValidator : AbstractValidator<SampleStorageOptions>
{
    public SampleStorageOptionsValidator()
    {
        RuleFor(x => x.RootPath)
            .NotEmpty()
            .Matches(@"^/");

        RuleFor(x => x.MaxFileSizeBytes)
            .InclusiveBetween(1, 10_000_000);
    }
}
