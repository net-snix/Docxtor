using Docxtor.Core.Models;

namespace Docxtor.Cli.Cli;

internal sealed class AppRunJobFactory
{
    public (MergeJob? Job, string? Error) Build(AppRunRequest? request, string requestDirectory)
    {
        if (request is null)
        {
            return (null, "Request payload is missing.");
        }

        if (request.Inputs.Length == 0 || request.Inputs.Any(path => string.IsNullOrWhiteSpace(path)))
        {
            return (null, "Request must include at least one input DOCX path.");
        }

        if (string.IsNullOrWhiteSpace(request.OutputPath))
        {
            return (null, "Request must include an outputPath.");
        }

        if (string.IsNullOrWhiteSpace(request.ReportPath))
        {
            return (null, "Request must include a reportPath.");
        }

        var options = new CommandLineOptions
        {
            Inputs = request.Inputs.ToList(),
            OutputPath = request.OutputPath,
            ReportPath = request.ReportPath,
            TemplatePath = request.TemplatePath,
            Backend = "openxml-sdk",
            PreserveSections = true,
            PreserveHeadersFooters = true,
            NumberingMode = NumberingMode.PreserveSource,
            TrackedChangesMode = TrackedChangesMode.Fail,
            AltChunkMode = AltChunkMode.Reject,
            ValidateOpenXml = true,
            ValidateReferences = true,
            VisualQa = false,
            EmitReport = true,
            FailOnWarnings = false,
        };

        var (job, _, error) = new JobFactory().Build(options, manifest: null, requestDirectory);
        if (job is null || error is not null)
        {
            return (job, error);
        }

        return (
            job with
            {
                Policy = job.Policy with
                {
                    InsertSourceFileTitles = request.InsertSourceFileTitles,
                },
            },
            null);
    }
}
