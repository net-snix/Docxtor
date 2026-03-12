namespace Docxtor.Core.Models;

public enum MergeStage
{
    Starting,
    Preflight,
    MergingInput,
    Validation,
    WritingReport,
    Completed,
}

public sealed record MergeProgressUpdate
{
    public required MergeStage Stage { get; init; }

    public int? CurrentInputIndex { get; init; }

    public int? TotalInputs { get; init; }

    public string? InputDisplayName { get; init; }
}
