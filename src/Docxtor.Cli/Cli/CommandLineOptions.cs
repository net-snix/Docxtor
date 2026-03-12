using Docxtor.Core.Models;

namespace Docxtor.Cli.Cli;

internal sealed record CommandLineOptions
{
    public List<string> Inputs { get; init; } = [];

    public string? ConfigPath { get; init; }

    public string? OutputPath { get; init; }

    public string? ReportPath { get; init; }

    public string? TemplatePath { get; init; }

    public string? Backend { get; init; }

    public BoundaryMode? BoundaryMode { get; init; }

    public bool? PreserveSections { get; init; }

    public bool? PreserveHeadersFooters { get; init; }

    public NumberingMode? NumberingMode { get; init; }

    public TrackedChangesMode? TrackedChangesMode { get; init; }

    public AltChunkMode? AltChunkMode { get; init; }

    public ThemePolicy? ThemePolicy { get; init; }

    public ExternalResourceMode? ExternalResourceMode { get; init; }

    public bool? ImageDeduplication { get; init; }

    public bool? UpdateFieldsOnOpen { get; init; }

    public bool? ValidateOpenXml { get; init; }

    public bool? ValidateReferences { get; init; }

    public bool? VisualQa { get; init; }

    public bool? FailOnWarnings { get; init; }

    public bool? EmitReport { get; init; }

    public LogFormat? LogFormat { get; init; }

    public bool DryRun { get; init; }

    public bool ShowHelp { get; init; }

    public bool ShowVersion { get; init; }
}
