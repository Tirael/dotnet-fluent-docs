using FluentDocs;
using FluentValidation;
using FluentValidation.Validators;

namespace FluentDocs.Tests.Fixtures;

public enum SampleStatus
{
    Ok,
    Error
}

/// <summary>
/// Фикстура для оставшихся встроенных правил FluentValidation.
/// </summary>
[SettingsDocs("Remaining")]
public sealed class SampleRemainingRulesOptions
{
    /// <summary>
    /// Отображаемое имя.
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// Пароль.
    /// </summary>
    public string Password { get; set; } = "secret";

    /// <summary>
    /// Подтверждение пароля.
    /// </summary>
    public string PasswordConfirmation { get; set; } = "secret";

    /// <summary>
    /// Строковое имя статуса.
    /// </summary>
    public string StatusName { get; set; } = "Ok";

    /// <summary>
    /// Статус.
    /// </summary>
    public SampleStatus Status { get; set; } = SampleStatus.Ok;

    /// <summary>
    /// Сумма.
    /// </summary>
    public decimal Amount { get; set; } = 1.25m;

    /// <summary>
    /// Оценка.
    /// </summary>
    public int Score { get; set; } = 5;

    /// <summary>
    /// Номер карты.
    /// </summary>
    public string CardNumber { get; set; } = "4111111111111111";

    /// <summary>
    /// Метки.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Устаревшее поле.
    /// </summary>
    public string? Deprecated { get; set; }

    /// <summary>
    /// Код.
    /// </summary>
    public string Code { get; set; } = "A1";

    /// <summary>
    /// Полиморфный контакт.
    /// </summary>
    public SampleContact? Contact { get; set; }
}

public abstract class SampleContact
{
    public string Label { get; set; } = "";
}

public sealed class SampleEmailContact : SampleContact
{
    public string Email { get; set; } = "";
}

public sealed class SamplePhoneContact : SampleContact
{
    public string Phone { get; set; } = "";
}

public sealed class SampleRemainingRulesOptionsValidator : AbstractValidator<SampleRemainingRulesOptions>
{
    public SampleRemainingRulesOptionsValidator()
    {
        RuleFor(x => x.Nickname)
            .Null()
            .When(x => x.Deprecated is not null);

        RuleFor(x => x.Deprecated)
            .Empty();

        RuleFor(x => x.Password)
            .NotEqual("password")
            .Equal(x => x.PasswordConfirmation);

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.StatusName)
            .IsEnumName(typeof(SampleStatus), caseSensitive: false);

        RuleFor(x => x.Amount)
            .PrecisionScale(4, 2, ignoreTrailingZeros: true)
            .ExclusiveBetween(0m, 100m);

        RuleFor(x => x.Score)
            .InclusiveBetween(1, 10);

        RuleFor(x => x.CardNumber)
            .CreditCard()
            .Must(card => card.StartsWith('4'))
            .Custom((card, context) =>
            {
                if (card.Contains(' '))
                    context.AddFailure("Пробелы в номере карты недопустимы.");
            });

        RuleFor(x => x.Tags)
            .NotNull()
            .ForEach(tag =>
            {
                tag.NotEmpty();
                tag.MaximumLength(16);
                tag.Matches("^[a-z0-9-]+$");
            });

        RuleFor(x => x.Code)
            .SetValidator(new SamplePrefixValidator<SampleRemainingRulesOptions>());

        RuleFor(x => x.Contact)
            .SetInheritanceValidator(v =>
            {
                v.Add(new SampleEmailContactValidator());
                v.Add(new SamplePhoneContactValidator());
            });
    }
}

public sealed class SampleEmailContactValidator : AbstractValidator<SampleEmailContact>
{
    public SampleEmailContactValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class SamplePhoneContactValidator : AbstractValidator<SamplePhoneContact>
{
    public SamplePhoneContactValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^\+?[0-9]{10,15}$");
    }
}

public sealed class SamplePrefixValidator<T> : PropertyValidator<T, string>
{
    public override string Name => "StartsWithA";

    public override bool IsValid(ValidationContext<T> context, string value)
        => value.StartsWith('A');

    protected override string GetDefaultMessageTemplate(string errorCode)
        => "Должно начинаться с A.";
}
