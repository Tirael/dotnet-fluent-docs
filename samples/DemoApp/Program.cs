using DemoApp.Options;
using DemoApp.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddSingleton<IValidator<MailOptions>, MailOptionsValidator>();
builder.Services.AddSingleton<IValidator<StorageOptions>, StorageOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<MailOptions>, FluentValidationValidateOptions<MailOptions>>();
builder.Services.AddSingleton<IValidateOptions<StorageOptions>, FluentValidationValidateOptions<StorageOptions>>();
builder.Services.AddOptions<MailOptions>().BindConfiguration("Mail").ValidateOnStart();
builder.Services.AddOptions<StorageOptions>().BindConfiguration("Storage").ValidateOnStart();

using var host = builder.Build();
var mail = host.Services.GetRequiredService<IOptions<MailOptions>>().Value;
var storage = host.Services.GetRequiredService<IOptions<StorageOptions>>().Value;

Console.WriteLine($"Mail: {mail.Host}:{mail.Port} from {mail.From}");
Console.WriteLine($"Storage: {storage.Provider} at {storage.RootPath}");
