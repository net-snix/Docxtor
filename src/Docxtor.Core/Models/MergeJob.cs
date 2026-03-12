namespace Docxtor.Core.Models;

public sealed record MergeJob
{
    public required IReadOnlyList<InputDocument> Inputs { get; init; }

    public string OutputPath { get; init; } = Path.GetFullPath("main.docx");

    public string ReportPath { get; init; } = Path.GetFullPath("merge-report.json");

    public MergePolicy Policy { get; init; } = new();

    public ValidationPolicy Validation { get; init; } = new();

    public string? BackendHint { get; init; }

    public string? TemplatePath { get; init; }

    public string? WorkingDirectory { get; init; }

    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");

    public bool DryRun { get; init; }
}
