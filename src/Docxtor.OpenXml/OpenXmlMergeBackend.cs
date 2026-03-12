using Docxtor.Core.Abstractions;
using Docxtor.Core.Models;
using Docxtor.OpenXml.Merge;
using Docxtor.OpenXml.Preflight;
using Docxtor.Validation;

namespace Docxtor.OpenXml;

public sealed class OpenXmlMergeBackend : IMergeBackend
{
    private readonly OpenXmlMergeExecutor _executor;
    private readonly OpenXmlPreflightInspector _inspector;

    public OpenXmlMergeBackend()
    {
        var capabilities = new BackendCapabilities
        {
            SupportsPreserveSections = true,
            SupportsHeadersFooters = true,
            SupportsNotesComments = true,
            SupportsTextBoxes = true,
            SupportsCharts = false,
            SupportsSmartArt = false,
            SupportsEmbeddedObjects = false,
            SupportsTrackedChangesNormalization = false,
            SupportsAltChunkResolution = false,
            SupportsContinueDestinationNumbering = false,
            SupportsVisualQa = false,
            SupportedBoundaryModes = new HashSet<BoundaryMode>
            {
                BoundaryMode.SectionNewPage,
                BoundaryMode.ContinuousSection,
                BoundaryMode.PageBreak,
                BoundaryMode.None,
            },
        };

        Name = "openxml-sdk";
        Version = typeof(OpenXmlMergeBackend).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        var styleMerger = new StyleMerger();
        var numberingMerger = new NumberingMerger();
        var relationshipCopier = new RelationshipCopier();
        var idNormalizer = new IdNormalizer();
        var notesCommentsMerger = new NotesCommentsMerger(
            styleMerger,
            numberingMerger,
            relationshipCopier,
            idNormalizer);
        var sectionMerger = new SectionMerger(relationshipCopier);

        _inspector = new OpenXmlPreflightInspector(Name, Version, capabilities);
        _executor = new OpenXmlMergeExecutor(
            new OpenXmlSchemaValidator(),
            new ReferentialIntegrityValidator(),
            new VisualQaValidator(),
            styleMerger,
            numberingMerger,
            relationshipCopier,
            notesCommentsMerger,
            sectionMerger,
            idNormalizer);
        Capabilities = capabilities;
    }

    private BackendCapabilities Capabilities { get; }

    public string Name { get; }

    public string Version { get; }

    public BackendCapabilities GetCapabilities() => Capabilities;

    public Task<PreflightResult> InspectAsync(
        IReadOnlyList<InputDocument> inputs,
        MergePolicy policy,
        CancellationToken cancellationToken = default)
    {
        return _inspector.InspectAsync(inputs, policy, cancellationToken);
    }

    public async Task<MergeResult> MergeAsync(
        MergeJob job,
        CancellationToken cancellationToken = default)
        => await MergeAsync(job, progress: null, cancellationToken);

    public async Task<MergeResult> MergeAsync(
        MergeJob job,
        IProgress<MergeProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var preflight = await _inspector.InspectAsync(job.Inputs, job.Policy, cancellationToken);
        if (!preflight.Success)
        {
            return new MergeResult
            {
                Success = false,
                FailureCode = FailureCode.PreflightCapabilityFailure,
                Report = new MergeReport
                {
                    CorrelationId = job.CorrelationId,
                    Status = "Failed",
                    StartedAtUtc = DateTimeOffset.UtcNow,
                    FinishedAtUtc = DateTimeOffset.UtcNow,
                    OutputPath = job.OutputPath,
                    Backend = Name,
                    Policy = job.Policy,
                    InputSummaries = job.Inputs,
                    PreflightInventories = preflight.Inputs,
                    PreflightWarnings = preflight.Warnings,
                    Errors = preflight.Errors,
                    FailureCode = FailureCode.PreflightCapabilityFailure,
                },
            };
        }

        return await _executor.ExecuteAsync(job, preflight, progress, cancellationToken);
    }
}
