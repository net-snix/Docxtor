namespace Docxtor.Core.Models;

public sealed record PreflightResult
{
    public bool Success { get; init; }

    public required string Backend { get; init; }

    public required string BackendVersion { get; init; }

    public IReadOnlyList<FeatureInventory> Inputs { get; init; } = [];

    public IReadOnlyList<DiagnosticMessage> Warnings { get; init; } = [];

    public IReadOnlyList<DiagnosticMessage> Errors { get; init; } = [];
}
