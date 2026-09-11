using FluentDocs;
using FluentValidation;

namespace FluentDocs.Tests.Fixtures;

/// <summary>
/// SMTP-настройки для фикстуры генератора.
/// </summary>
[SettingsDocs("Mail")]
public sealed class SampleMailOptions
{
    /// <summary>
    /// Имя SMTP-хоста.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Порт SMTP.
    /// </summary>
    public int Port { get; set; } = 25;

    /// <summary>
    /// Адрес отправителя.
    /// </summary>
    public string From { get; set; } = "noreply@localhost";

    /// <summary>
    /// Включает TLS после подключения.
    /// </summary>
    public bool EnableTls { get; set; }

    /// <summary>
    /// Вложенная политика повторов.
    /// </summary>
    public SampleRetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Получатели уведомлений.
    /// </summary>
    public List<SampleRecipientOptions> Recipients { get; set; } = [];
}

/// <summary>
/// Фикстура политики повторов.
/// </summary>
public sealed class SampleRetryOptions
{
    /// <summary>
    /// Максимальное число попыток.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Пауза в миллисекундах.
    /// </summary>
    public int DelayMilliseconds { get; set; } = 200;
}

/// <summary>
/// Фикстура получателя.
/// </summary>
public sealed class SampleRecipientOptions
{
    /// <summary>
    /// Адрес электронной почты.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Отображаемое имя.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Фикстура настроек хранилища.
/// </summary>
[SettingsDocs("Storage")]
public sealed class SampleStorageOptions
{
    /// <summary>
    /// Абсолютный корневой путь.
    /// </summary>
    public string RootPath { get; set; } = "/data";

    /// <summary>
    /// Максимальный размер файла в байтах.
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
            .WithMessage("SMTP-хост обязателен и не длиннее 255 символов.");

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
