using Docxtor.Cli.Cli;

namespace Docxtor.UnitTests;

public sealed class JobFactoryTests
{
    [Fact]
    public void Build_rejects_output_path_that_matches_input()
    {
        using var sandbox = new TemporaryDirectory();
        var inputPath = Path.Combine(sandbox.Path, "source.docx");

        var (job, _, error) = new JobFactory().Build(
            new CommandLineOptions
            {
                Inputs = [inputPath],
                OutputPath = inputPath,
            },
            manifest: null,
            sandbox.Path);

        Assert.Null(job);
        Assert.Equal(
            $"Output path '{inputPath}' must be different from input '{inputPath}'.",
            error);
    }

    [Fact]
    public void Build_rejects_report_path_that_matches_output()
    {
        using var sandbox = new TemporaryDirectory();
        var outputPath = Path.Combine(sandbox.Path, "merged.docx");

        var (job, _, error) = new JobFactory().Build(
            new CommandLineOptions
            {
                Inputs = [Path.Combine(sandbox.Path, "source.docx")],
                OutputPath = outputPath,
                ReportPath = outputPath,
            },
            manifest: null,
            sandbox.Path);

        Assert.Null(job);
        Assert.Equal("Output path and report path must be different files.", error);
    }

    [Fact]
    public void Build_rejects_template_path_that_matches_input()
    {
        using var sandbox = new TemporaryDirectory();
        var inputPath = Path.Combine(sandbox.Path, "source.docx");

        var (job, _, error) = new JobFactory().Build(
            new CommandLineOptions
            {
                Inputs = [inputPath],
                OutputPath = Path.Combine(sandbox.Path, "merged.docx"),
                ReportPath = Path.Combine(sandbox.Path, "merge-report.json"),
                TemplatePath = inputPath,
            },
            manifest: null,
            sandbox.Path);

        Assert.Null(job);
        Assert.Equal("Template path must be different from every input DOCX.", error);
    }

    [Fact]
    public void Build_rejects_output_path_that_is_a_symlink_to_input()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var sandbox = new TemporaryDirectory();
        var inputPath = Path.Combine(sandbox.Path, "source.docx");
        var outputAliasPath = Path.Combine(sandbox.Path, "output-alias.docx");
        File.WriteAllText(inputPath, "input");
        File.CreateSymbolicLink(outputAliasPath, inputPath);

        var (job, _, error) = new JobFactory().Build(
            new CommandLineOptions
            {
                Inputs = [inputPath],
                OutputPath = outputAliasPath,
            },
            manifest: null,
            sandbox.Path);

        Assert.Null(job);
        Assert.Equal(
            $"Output path '{outputAliasPath}' must be different from input '{inputPath}'.",
            error);
    }

    [Fact]
    public void Build_rejects_template_path_that_is_a_symlink_to_input()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var sandbox = new TemporaryDirectory();
        var inputPath = Path.Combine(sandbox.Path, "source.docx");
        var templateAliasPath = Path.Combine(sandbox.Path, "template-alias.docx");
        File.WriteAllText(inputPath, "input");
        File.CreateSymbolicLink(templateAliasPath, inputPath);

        var (job, _, error) = new JobFactory().Build(
            new CommandLineOptions
            {
                Inputs = [inputPath],
                OutputPath = Path.Combine(sandbox.Path, "merged.docx"),
                ReportPath = Path.Combine(sandbox.Path, "merge-report.json"),
                TemplatePath = templateAliasPath,
            },
            manifest: null,
            sandbox.Path);

        Assert.Null(job);
        Assert.Equal("Template path must be different from every input DOCX.", error);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"docxtor-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
