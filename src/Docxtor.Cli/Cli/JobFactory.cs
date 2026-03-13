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

        var boundaryMode = options.BoundaryMode ?? MergeOptionParsers.ParseBoundaryMode(manifest?.Merge.Boundary) ?? BoundaryMode.SectionNewPage;
        var numberingMode = options.NumberingMode ?? MergeOptionParsers.ParseNumberingMode(manifest?.Merge.Numbering) ?? NumberingMode.PreserveSource;
        var trackedChangesMode = options.TrackedChangesMode ?? MergeOptionParsers.ParseTrackedChangesMode(manifest?.Merge.TrackedChanges) ?? TrackedChangesMode.Fail;
        var altChunkMode = options.AltChunkMode ?? MergeOptionParsers.ParseAltChunkMode(manifest?.Merge.AltChunk) ?? AltChunkMode.Reject;
        var themePolicy = options.ThemePolicy ?? MergeOptionParsers.ParseThemePolicy(manifest?.Merge.ThemePolicy) ?? ThemePolicy.BaseWins;
        var externalResourceMode = options.ExternalResourceMode ?? MergeOptionParsers.ParseExternalResourceMode(manifest?.Merge.ExternalResources) ?? ExternalResourceMode.PreserveLinks;
        var logFormat = options.LogFormat ?? MergeOptionParsers.ParseLogFormat(manifest?.Report.LogFormat) ?? LogFormat.Text;
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
}
