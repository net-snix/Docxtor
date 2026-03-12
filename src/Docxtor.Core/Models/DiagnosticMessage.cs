namespace Docxtor.Core.Models;

public sealed record DiagnosticMessage
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? InputPath { get; init; }

    public string? PartUri { get; init; }
}
