namespace Docxtor.Core.Models;

public sealed record ValidationSummary
{
    public ValidationOutcome Outcome { get; init; } = ValidationOutcome.Skipped;

    public IReadOnlyList<DiagnosticMessage> Messages { get; init; } = [];
}
