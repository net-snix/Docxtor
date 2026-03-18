using Docxtor.Cli.Cli;
using Docxtor.Core.Models;

namespace Docxtor.UnitTests;

public sealed class CommandLineParserTests
{
    [Fact]
    public void Parse_reads_all_mode_options()
    {
        var (options, error) = new CommandLineParser().Parse(
        [
            "--boundary", "continuous-section",
            "--numbering", "continue-destination",
            "--tracked-changes", "accept-all",
            "--altchunk", "resolve",
            "--theme-policy", "template-wins",
            "--external-resources", "materialize",
            "--log-format", "json",
            "source.docx",
        ]);

        Assert.Null(error);
        Assert.NotNull(options);
        Assert.Equal(BoundaryMode.ContinuousSection, options!.BoundaryMode);
        Assert.Equal(NumberingMode.ContinueDestination, options.NumberingMode);
        Assert.Equal(TrackedChangesMode.AcceptAll, options.TrackedChangesMode);
        Assert.Equal(AltChunkMode.Resolve, options.AltChunkMode);
        Assert.Equal(ThemePolicy.TemplateWins, options.ThemePolicy);
        Assert.Equal(ExternalResourceMode.Materialize, options.ExternalResourceMode);
        Assert.Equal(LogFormat.Json, options.LogFormat);
    }

    [Fact]
    public void Parse_rejects_unknown_boundary_mode()
    {
        var (_, error) = new CommandLineParser().Parse(["--boundary", "sideways"]);

        Assert.Equal("Unknown boundary mode 'sideways'.", error);
    }

    [Fact]
    public void Parse_rejects_missing_log_format_value()
    {
        var (_, error) = new CommandLineParser().Parse(["--log-format"]);

        Assert.Equal("Option '--log-format' requires a value.", error);
    }
}
