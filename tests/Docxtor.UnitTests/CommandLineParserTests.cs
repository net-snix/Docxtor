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
}
