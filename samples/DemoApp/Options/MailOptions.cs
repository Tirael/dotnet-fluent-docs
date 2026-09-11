using FluentDocs;

namespace DemoApp.Options;

/// <summary>
/// Настройки SMTP-отправки почты.
/// </summary>
/// <remarks>
/// Привязываются к секции конфигурации <c>Mail</c>.
/// </remarks>
[SettingsDocs("Mail")]
public sealed class MailOptions
{
    /// <summary>
    /// Имя или адрес SMTP-хоста.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// TCP-порт SMTP.
    /// </summary>
    public int Port { get; set; } = 25;

    /// <summary>
    /// Адрес отправителя исходящей почты.
    /// </summary>
    public string From { get; set; } = "noreply@localhost";

    /// <summary>
    /// Если true, после подключения клиент поднимает TLS.
    /// </summary>
    public bool EnableTls { get; set; } = true;

    /// <summary>
    /// Таймаут отправки в секундах.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Политика повторов после неудачной отправки.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Получатели, которые всегда получают копию системных уведомлений.
    /// </summary>
    public List<RecipientOptions> Recipients { get; set; } = [];

    /// <summary>
    /// Регулярные выражения, которым должны соответствовать адреса отправителя.
    /// </summary>
    public List<string> AllowedSenderPatterns { get; set; } = [];
}

/// <summary>
/// Политика повторов при временных сбоях отправки почты.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Максимальное число попыток отправки, включая первую.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Пауза между попытками в миллисекундах.
    /// </summary>
    public int DelayMilliseconds { get; set; } = 200;
}

/// <summary>
/// Получатель уведомления.
/// </summary>
public sealed class RecipientOptions
{
    /// <summary>
    /// Адрес электронной почты получателя.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Необязательное отображаемое имя.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Настройки файлового хранилища демо-артефактов.
/// </summary>
[SettingsDocs("Storage")]
public sealed class StorageOptions
{
    /// <summary>
    /// Идентификатор хранилища. Допустимые значения: Local, S3.
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Абсолютный каталог, если <see cref="Provider"/> равен Local.
    /// </summary>
    public string RootPath { get; set; } = "/var/demo/data";

    /// <summary>
    /// Максимальный размер загружаемого файла в байтах.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 1_048_576;

    /// <summary>
    /// Имя бакета объектного хранилища. Обязательно, если <see cref="Provider"/> равен S3.
    /// </summary>
    public string? BucketName { get; set; }
}
