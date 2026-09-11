using System.Diagnostics;
using FluentDocs.Rendering;

namespace FluentDocs.Tests;

public sealed class DocumentationPipelineTests
{
    [Fact]
    public void Run_writes_markdown_and_snapshot_with_changelog()
    {
        var assembly = typeof(Fixtures.SampleMailOptions).Assembly;
        var xml = Path.ChangeExtension(assembly.Location, ".xml");
        using var directory = new TempDirectory();

        var firstOutput = Path.Combine(directory.Path, "settings.md");
        var snapshot = Path.Combine(directory.Path, "settings.snapshot.json");

        var first = DocumentationPipeline.Run(new DocumentationRequest
        {
            Assembly = assembly,
            AssemblyPath = assembly.Location,
            XmlDocumentationPath = xml,
            OutputPath = firstOutput,
            SnapshotPath = snapshot
        });

        Assert.True(first.IsInitial);
        Assert.Contains("Initial catalog generated.", first.Markdown, StringComparison.Ordinal);
        Assert.True(File.Exists(firstOutput));
        Assert.True(File.Exists(snapshot));

        var previous = SnapshotSerializer.Deserialize(File.ReadAllText(snapshot));
        var host = previous.Types.Single(t => t.Name == "SampleMailOptions").Properties.Single(p => p.Path == "Host");
        host.Rules.RemoveAll(r => r.Id == "MaximumLength:255");
        host.Rules.Add(new SettingsRuleDocument { Id = "MaximumLength:128", Description = "Maximum length is 128." });
        File.WriteAllText(snapshot, SnapshotSerializer.Serialize(previous));

        var second = DocumentationPipeline.Run(new DocumentationRequest
        {
            Assembly = assembly,
            AssemblyPath = assembly.Location,
            XmlDocumentationPath = xml,
            OutputPath = firstOutput,
            SnapshotPath = snapshot
        });

        Assert.False(second.IsInitial);
        Assert.Contains("Added constraint `MaximumLength:255`", second.Markdown, StringComparison.Ordinal);
        Assert.Contains("Removed constraint `MaximumLength:128`", second.Markdown, StringComparison.Ordinal);
    }
}

public sealed class DemoAppBuildTests
{
    [Fact]
    public void Building_demo_app_generates_settings_documentation()
    {
        var repo = FindRepoRoot();
        var demo = Path.Combine(repo, "samples", "DemoApp");
        var start = new ProcessStartInfo("dotnet", "build --nologo -v:m")
        {
            WorkingDirectory = demo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(start);
        Assert.NotNull(process);
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"dotnet build failed.\n{stdout}\n{stderr}");

        var markdownPath = Path.Combine(demo, "docs", "settings.md");
        var snapshotPath = Path.Combine(demo, "docs", "settings.snapshot.json");
        Assert.True(File.Exists(markdownPath), stdout);
        Assert.True(File.Exists(snapshotPath), stdout);

        var markdown = File.ReadAllText(markdownPath);
        Assert.Contains("MailOptions", markdown, StringComparison.Ordinal);
        Assert.Contains("StorageOptions", markdown, StringComparison.Ordinal);
        Assert.Contains("MaximumLength:255", markdown, StringComparison.Ordinal);
        Assert.Contains("Retry.MaxAttempts", markdown, StringComparison.Ordinal);
        Assert.Contains("Recipients[].Email", markdown, StringComparison.Ordinal);
        Assert.Contains("SMTP host name or address.", markdown, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "dotnet-fluent-docs.sln"))
                || File.Exists(Path.Combine(directory.FullName, "dotnet-fluent-docs.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
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
            // Best-effort cleanup for locked files on some agents.
        }
    }
}
