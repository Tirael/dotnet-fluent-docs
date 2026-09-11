using FluentDocs;

namespace DemoApp.Options;

/// <summary>
/// SMTP mail delivery settings.
/// </summary>
/// <remarks>
/// Bound to the <c>Mail</c> configuration section.
/// </remarks>
[SettingsDocs("Mail")]
public sealed class MailOptions
{
    /// <summary>
    /// SMTP host name or address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// SMTP TCP port.
    /// </summary>
    public int Port { get; set; } = 25;

    /// <summary>
    /// Envelope sender address used for outgoing mail.
    /// </summary>
    public string From { get; set; } = "noreply@localhost";

    /// <summary>
    /// When true, the client starts a TLS session after connect.
    /// </summary>
    public bool EnableTls { get; set; } = true;

    /// <summary>
    /// Send timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Retry policy applied after a failed send.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Recipients that always receive a copy of system notifications.
    /// </summary>
    public List<RecipientOptions> Recipients { get; set; } = [];

    /// <summary>
    /// Regular expressions that outgoing sender addresses must match.
    /// </summary>
    public List<string> AllowedSenderPatterns { get; set; } = [];
}

/// <summary>
/// Retry policy for transient mail failures.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Maximum number of send attempts, including the first try.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between attempts in milliseconds.
    /// </summary>
    public int DelayMilliseconds { get; set; } = 200;
}

/// <summary>
/// A notification recipient.
/// </summary>
public sealed class RecipientOptions
{
    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional display name.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// File storage settings for demo artifacts.
/// </summary>
[SettingsDocs("Storage")]
public sealed class StorageOptions
{
    /// <summary>
    /// Storage backend identifier. Supported values: Local, S3.
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Absolute directory used when <see cref="Provider"/> is Local.
    /// </summary>
    public string RootPath { get; set; } = "/var/demo/data";

    /// <summary>
    /// Maximum uploaded file size in bytes.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 1_048_576;

    /// <summary>
    /// Object-storage bucket name. Required when <see cref="Provider"/> is S3.
    /// </summary>
    public string? BucketName { get; set; }
}
