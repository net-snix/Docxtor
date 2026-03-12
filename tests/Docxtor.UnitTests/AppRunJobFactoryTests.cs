using Docxtor.Cli.Cli;
using Docxtor.Core.Models;

namespace Docxtor.UnitTests;

public sealed class AppRunJobFactoryTests
{
    [Fact]
    public void Build_maps_request_into_fixed_v1_job_defaults()
    {
        var requestDirectory = Path.Combine(Path.GetTempPath(), $"docxtor-apprun-{Guid.NewGuid():N}");
        var request = new AppRunRequest
        {
            Inputs = ["inputs/one.docx", "inputs/two.docx"],
            OutputPath = "out/main.docx",
            ReportPath = "out/main.merge-report.json",
            TemplatePath = "templates/base.docx",
            InsertSourceFileTitles = true,
        };

        var (job, error) = new AppRunJobFactory().Build(request, requestDirectory);

        Assert.Null(error);
        Assert.NotNull(job);
        Assert.Equal("openxml-sdk", job!.BackendHint);
        Assert.Equal(Path.Combine(requestDirectory, "out/main.docx"), job.OutputPath);
        Assert.Equal(Path.Combine(requestDirectory, "out/main.merge-report.json"), job.ReportPath);
        Assert.Equal(Path.Combine(requestDirectory, "templates/base.docx"), job.TemplatePath);
        Assert.Equal(requestDirectory, job.WorkingDirectory);
        Assert.Equal(
            [Path.Combine(requestDirectory, "inputs/one.docx"), Path.Combine(requestDirectory, "inputs/two.docx")],
            job.Inputs.Select(input => input.PathOrId).ToArray());
        Assert.Equal(SectionPolicy.PreserveSourceSections, job.Policy.SectionPolicy);
        Assert.True(job.Policy.PreserveHeadersFooters);
        Assert.Equal(NumberingMode.PreserveSource, job.Policy.NumberingMode);
        Assert.Equal(TrackedChangesMode.Fail, job.Policy.TrackedChangesMode);
        Assert.Equal(AltChunkMode.Reject, job.Policy.AltChunkMode);
        Assert.True(job.Policy.InsertSourceFileTitles);
        Assert.True(job.Validation.RunOpenXmlValidation);
        Assert.True(job.Validation.RunReferentialIntegrityChecks);
        Assert.False(job.Validation.RunVisualRegression);
        Assert.True(job.Validation.EmitReport);
        Assert.False(job.Validation.FailOnWarnings);
    }

    [Fact]
    public void Build_rejects_missing_output_path()
    {
        var (job, error) = new AppRunJobFactory().Build(
            new AppRunRequest
            {
                Inputs = ["one.docx"],
                ReportPath = "merge-report.json",
            },
            Directory.GetCurrentDirectory());

        Assert.Null(job);
        Assert.Equal("Request must include an outputPath.", error);
    }
}
