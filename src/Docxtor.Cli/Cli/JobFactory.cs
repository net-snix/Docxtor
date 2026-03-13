using Docxtor.Core.Models;

namespace Docxtor.Cli.Cli;

internal sealed class JobFactory
{
    public (MergeJob? Job, LogFormat LogFormat, string? Error) Build(
        CommandLineOptions options,
        ManifestFileModel? manifest,
        string workingDirectory)
    {
        var inputs = (options.Inputs.Count > 0 ? options.Inputs : manifest?.Inputs ?? [])
            .Select((path, index) => InputDocument.FromPath(Path.GetFullPath(path, workingDirectory), index))
            .ToArray();

        if (inputs.Length == 0)
        {
            return (null, LogFormat.Text, "At least one input DOCX is required.");
        }

        var boundaryMode = options.BoundaryMode ?? ParseBoundary(manifest?.Merge.Boundary) ?? BoundaryMode.SectionNewPage;
        var numberingMode = options.NumberingMode ?? ParseNumbering(manifest?.Merge.Numbering) ?? NumberingMode.PreserveSource;
        var trackedChangesMode = options.TrackedChangesMode ?? ParseTrackedChanges(manifest?.Merge.TrackedChanges) ?? TrackedChangesMode.Fail;
        var altChunkMode = options.AltChunkMode ?? ParseAltChunk(manifest?.Merge.AltChunk) ?? AltChunkMode.Reject;
        var themePolicy = options.ThemePolicy ?? ParseThemePolicy(manifest?.Merge.ThemePolicy) ?? ThemePolicy.BaseWins;
        var externalResourceMode = options.ExternalResourceMode ?? ParseExternalMode(manifest?.Merge.ExternalResources) ?? ExternalResourceMode.PreserveLinks;
        var logFormat = options.LogFormat ?? ParseLogFormat(manifest?.Report.LogFormat) ?? LogFormat.Text;
        var preserveSections = options.PreserveSections ?? manifest?.Merge.PreserveSections ?? true;
        var sectionPolicy = preserveSections ? SectionPolicy.PreserveSourceSections : SectionPolicy.UnifyWithBaseHeadersFooters;
        var emitReport = options.EmitReport ?? manifest?.Validation.EmitReport ?? true;

        var outputPath = Path.GetFullPath(options.OutputPath ?? manifest?.Output ?? "main.docx", workingDirectory);
        var reportPath = Path.GetFullPath(options.ReportPath ?? manifest?.Report.Path ?? "merge-report.json", workingDirectory);
        var templatePath = options.TemplatePath ?? manifest?.Template;
        templatePath = string.IsNullOrWhiteSpace(templatePath) ? null : Path.GetFullPath(templatePath, workingDirectory);
        var pathSafetyError = JobPathSafetyValidator.Validate(inputs, outputPath, reportPath, templatePath);
        if (pathSafetyError is not null)
        {
            return (null, logFormat, pathSafetyError);
        }

        var job = new MergeJob
        {
            Inputs = inputs,
            OutputPath = outputPath,
            ReportPath = reportPath,
            BackendHint = options.Backend ?? manifest?.Backend ?? "openxml-sdk",
            TemplatePath = templatePath,
            WorkingDirectory = workingDirectory,
            DryRun = options.DryRun,
            Policy = new MergePolicy
            {
                BoundaryMode = boundaryMode,
                SectionPolicy = sectionPolicy,
                PreserveHeadersFooters = options.PreserveHeadersFooters ?? manifest?.Merge.PreserveHeadersFooters ?? true,
                NumberingMode = numberingMode,
                TrackedChangesMode = trackedChangesMode,
                AltChunkMode = altChunkMode,
                ThemePolicy = themePolicy,
                ExternalResourceMode = externalResourceMode,
                ImageDeduplication = options.ImageDeduplication ?? manifest?.Merge.ImageDedup ?? true,
                UpdateFieldsOnOpen = options.UpdateFieldsOnOpen ?? manifest?.Merge.UpdateFieldsOnOpen ?? true,
            },
            Validation = new ValidationPolicy
            {
                RunOpenXmlValidation = options.ValidateOpenXml ?? manifest?.Validation.OpenXml ?? true,
                RunReferentialIntegrityChecks = options.ValidateReferences ?? manifest?.Validation.References ?? true,
                RunVisualRegression = options.VisualQa ?? manifest?.Validation.VisualQa ?? false,
                EmitReport = emitReport,
                FailOnWarnings = options.FailOnWarnings ?? manifest?.Validation.FailOnWarnings ?? false,
            },
        };

        return (job, logFormat, null);
    }

    private static BoundaryMode? ParseBoundary(string? value) => value switch
    {
        "section-new-page" => BoundaryMode.SectionNewPage,
        "page-break" => BoundaryMode.PageBreak,
        "continuous-section" => BoundaryMode.ContinuousSection,
        "none" => BoundaryMode.None,
        _ => null,
    };

    private static NumberingMode? ParseNumbering(string? value) => value switch
    {
        "preserve-source" => NumberingMode.PreserveSource,
        "continue-destination" => NumberingMode.ContinueDestination,
        _ => null,
    };

    private static TrackedChangesMode? ParseTrackedChanges(string? value) => value switch
    {
        "fail" => TrackedChangesMode.Fail,
        "accept-all" => TrackedChangesMode.AcceptAll,
        "reject-all" => TrackedChangesMode.RejectAll,
        _ => null,
    };

    private static AltChunkMode? ParseAltChunk(string? value) => value switch
    {
        "reject" => AltChunkMode.Reject,
        "resolve" => AltChunkMode.Resolve,
        _ => null,
    };

    private static ThemePolicy? ParseThemePolicy(string? value) => value switch
    {
        "base-wins" => ThemePolicy.BaseWins,
        "import-first" => ThemePolicy.ImportFirst,
        "template-wins" => ThemePolicy.TemplateWins,
        _ => null,
    };

    private static ExternalResourceMode? ParseExternalMode(string? value) => value switch
    {
        "preserve-links" => ExternalResourceMode.PreserveLinks,
        "materialize" => ExternalResourceMode.Materialize,
        _ => null,
    };

    private static LogFormat? ParseLogFormat(string? value) => value switch
    {
        "text" => LogFormat.Text,
        "json" => LogFormat.Json,
        _ => null,
    };
}
