using System.Diagnostics;
using FluentDocs.Tests.Fixtures;

namespace FluentDocs.Tests;

public sealed class DocumentationPipelineTests
{
    [Fact]
    public void Run_writes_markdown_and_snapshot_with_changelog()
    {
        // Дано
        var assembly = typeof(SampleMailOptions).Assembly;
        var xml = Path.ChangeExtension(assembly.Location, ".xml");
        using var directory = new TempDirectory();
        var firstOutput = Path.Combine(directory.Path, "settings.md");
        var snapshot = Path.Combine(directory.Path, "settings.snapshot.json");

        // Когда
        var first = DocumentationPipeline.Run(new DocumentationRequest
        {
            Assembly = assembly,
            AssemblyPath = assembly.Location,
            XmlDocumentationPath = xml,
            OutputPath = firstOutput,
            SnapshotPath = snapshot
        });

        // Тогда
        first.IsInitial.Should().BeTrue();
        first.Markdown.Should().Contain("Каталог сформирован впервые.");
        File.Exists(firstOutput).Should().BeTrue();
        File.Exists(snapshot).Should().BeTrue();

        // Дано
        var previous = SnapshotSerializer.Deserialize(File.ReadAllText(snapshot));
        var host = previous.Types.Single(t => t.Name == "SampleMailOptions").Properties.Single(p => p.Path == "Host");
        host.Rules.RemoveAll(r => r.Id == "MaximumLength:255");
        host.Rules.Add(new SettingsRuleDocument { Id = "MaximumLength:128", Description = "Максимальная длина: 128." });
        File.WriteAllText(snapshot, SnapshotSerializer.Serialize(previous));

        // Когда
        var second = DocumentationPipeline.Run(new DocumentationRequest
        {
            Assembly = assembly,
            AssemblyPath = assembly.Location,
            XmlDocumentationPath = xml,
            OutputPath = firstOutput,
            SnapshotPath = snapshot
        });

        // Тогда
        second.IsInitial.Should().BeFalse();
        second.Markdown.Should().Contain("Добавлено ограничение `MaximumLength:255`");
        second.Markdown.Should().Contain("Удалено ограничение `MaximumLength:128`");
    }
}

public sealed class DemoAppBuildTests
{
    [Fact]
    public void Building_demo_app_generates_settings_documentation()
    {
        // Дано
        var repo = FindRepoRoot();
        var demo = Path.Combine(repo, "samples", "DemoApp");
        var start = new ProcessStartInfo("dotnet", "build --nologo -v:m")
        {
            WorkingDirectory = demo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        // Когда
        using var process = Process.Start(start);
        process.Should().NotBeNull();
        var stdout = process!.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Тогда
        process.ExitCode.Should().Be(0, $"сборка завершилась с ошибкой.\n{stdout}\n{stderr}");

        var markdownPath = Path.Combine(demo, "docs", "settings.md");
        var snapshotPath = Path.Combine(demo, "docs", "settings.snapshot.json");
        File.Exists(markdownPath).Should().BeTrue(stdout);
        File.Exists(snapshotPath).Should().BeTrue(stdout);

        var snapshotText = File.ReadAllText(snapshotPath, System.Text.Encoding.UTF8);
        snapshotText.Should().Contain("Настройки SMTP-отправки почты.");
        snapshotText.Should().Contain("Не должно быть пустым.");
        snapshotText.Should().NotContain("\\u041");

        var markdown = File.ReadAllText(markdownPath);
        markdown.Should().Contain("MailOptions");
        markdown.Should().Contain("StorageOptions");
        markdown.Should().Contain("MaximumLength:255");
        markdown.Should().Contain("Retry.MaxAttempts");
        markdown.Should().Contain("Recipients[].Email");
        markdown.Should().Contain("Имя или адрес SMTP-хоста.");
        markdown.Should().Contain("## Журнал изменений");
        markdown.Should().Contain("# Настройки приложения");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "dotnet-features-changelogs.sln"))
                || File.Exists(Path.Combine(directory.FullName, "dotnet-features-changelogs.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Не удалось найти корень репозитория.");
    }
}

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fluentdocs-" + Guid.NewGuid().ToString("N"));

    public TempDirectory()
    {
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Лучшая попытка очистки, если файлы ещё заняты.
        }
    }
}
