using FluentDocs;
using FluentValidation;

namespace FluentDocs.Tests.Fixtures;

/// <summary>
/// Настройки с валидатором, у которого нет конструктора без параметров.
/// </summary>
[SettingsDocs("Di")]
public sealed class SampleDiOptions
{
    /// <summary>
    /// Имя профиля.
    /// </summary>
    public string ProfileName { get; set; } = "default";
}

/// <summary>
/// Валидатор с зависимостью в конструкторе — разбор идёт по исходнику, экземпляр не создаётся.
/// </summary>
public sealed class SampleDiOptionsValidator : AbstractValidator<SampleDiOptions>
{
    public SampleDiOptionsValidator(string requiredDependency)
    {
        _ = requiredDependency;
        ConfigureRules();
    }

    private void ConfigureRules()
    {
        RuleFor(x => x.ProfileName)
            .NotEmpty()
            .MaximumLength(32);
    }
}
