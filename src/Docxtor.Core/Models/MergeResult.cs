namespace Docxtor.Core.Models;

public sealed record MergeResult
{
    public bool Success { get; init; }

    public string OutputPath { get; init; } = string.Empty;

    public FailureCode FailureCode { get; init; }

    public required MergeReport Report { get; init; }
}
