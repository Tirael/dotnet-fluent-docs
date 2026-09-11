using FluentValidation;
using Microsoft.Extensions.Options;

namespace DemoApp.Validation;

/// <summary>
/// Adapts a FluentValidation validator to the Options validation pipeline.
/// </summary>
public sealed class FluentValidationValidateOptions<TOptions>(IValidator<TOptions> validator) : IValidateOptions<TOptions>
    where TOptions : class
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        var result = validator.Validate(options);
        return result.IsValid
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(result.Errors.Select(error => error.ErrorMessage));
    }
}
