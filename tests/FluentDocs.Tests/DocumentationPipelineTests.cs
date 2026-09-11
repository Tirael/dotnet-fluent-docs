using System.Diagnostics;
using FluentDocs.Tests.Fixtures;

namespace FluentDocs.Tests;

public sealed class DocumentationPipelineTests
{
    [Fact]
    public void Given_no_previous_snapshot_When_pipeline_runs_Then_initial_markdown_and_snapshot_are_written()
    {
        // Arrange
        var assembly = typeof(SampleMailOptions).Assembly;
        var xml = Path.ChangeExtension(assembly.Location, ".xml");
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "settings.md");
        var snapshot = Path.Combine(directory.Path, "settings.snapshot.json");

        // Act
        var result = DocumentationPipeline.Run(new DocumentationRequest
        {
            Assembly = assembly,
            AssemblyPath = assembly.Location,
            XmlDocumentationPath = xml,
            OutputPath = output,
            SnapshotPath = snapshot
        });

        // Assert
        result.IsInitial.Should().BeTrue();
        result.Markdown.Should().Contain("Каталог сформирован впервые.");
        File.Exists(output).Should().BeTrue();
        File.Exists(snapshot).Should().BeTrue();
    }

    [Fact]
    public void Given_snapshot_with_old_rule_When_pipeline_runs_Then_changelog_reports_constraint_change()
    {
        // Arrange
        var assembly = typeof(SampleMailOptions).Assembly;
        var xml = Path.ChangeExtension(assembly.Location, ".xml");
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "settings.md");
        var snapshot = Path.Combine(directory.Path, "settings.snapshot.json");
        DocumentationPipeline.Run(new DocumentationRequest
        {
            Assembly = assembly,
            AssemblyPath = assembly.Location,
            XmlDocumentationPath = xml,
            OutputPath = output,
            SnapshotPath = snapshot
        });
        var previous = SnapshotSerializer.ReadFromFile(snapshot);
        var host = previous.Types.Single(t => t.Name == "SampleMailOptions").Properties.Single(p => p.Path == "Host");
        host.Rules.RemoveAll(r => r.Id == "MaximumLength:255");
        host.Rules.Add(new SettingsRuleDocument { Id = "MaximumLength:128", Description = "Максимальная длина: 128." });
        SnapshotSerializer.WriteToFile(snapshot, previous);

        // Act
        var result = DocumentationPipeline.Run(new DocumentationRequest
        {
            Assembly = assembly,
            AssemblyPath = assembly.Location,
            XmlDocumentationPath = xml,
            OutputPath = output,
            SnapshotPath = snapshot
        });

        // Assert
        result.IsInitial.Should().BeFalse();
        result.Markdown.Should().Contain("Добавлено ограничение `MaximumLength:255`");
        result.Markdown.Should().Contain("Удалено ограничение `MaximumLength:128`");
    }
}

public sealed class DemoAppBuildTests
{
    [Fact]
    public void Given_demo_app_When_project_is_built_Then_russian_settings_documentation_is_generated()
    {
        // Arrange
        var repo = FindRepoRoot();
        var demo = Path.Combine(repo, "samples", "DemoApp");
        var start = new ProcessStartInfo("dotnet", "build --nologo -v:m")
        {
            WorkingDirectory = demo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        // Act
        using var process = Process.Start(start);
        process.Should().NotBeNull();
        var stdout = process!.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Assert
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
