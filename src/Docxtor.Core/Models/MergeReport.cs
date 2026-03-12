namespace Docxtor.Core.Models;

public sealed record MergeReport
{
    public required string CorrelationId { get; init; }

    public required string Status { get; set; }

    public DateTimeOffset StartedAtUtc { get; init; }

    public DateTimeOffset? FinishedAtUtc { get; set; }

    public long DurationMs { get; set; }

    public required string OutputPath { get; init; }

    public long? OutputSizeBytes { get; set; }

    public required string Backend { get; set; }

    public required MergePolicy Policy { get; init; }

    public IReadOnlyList<InputDocument> InputSummaries { get; init; } = [];

    public IReadOnlyList<FeatureInventory> PreflightInventories { get; set; } = [];

    public IReadOnlyList<DiagnosticMessage> PreflightWarnings { get; set; } = [];

    public IReadOnlyList<DiagnosticMessage> MergeWarnings { get; set; } = [];

    public RemapSummary RemapCounts { get; init; } = new();

    public ValidationSummary OpenXmlValidation { get; set; } = new();

    public ValidationSummary ReferentialIntegrityValidation { get; set; } = new();

    public ValidationSummary VisualQaValidation { get; set; } = new();

    public FailureCode FailureCode { get; set; }

    public IReadOnlyList<DiagnosticMessage> Errors { get; set; } = [];
}
