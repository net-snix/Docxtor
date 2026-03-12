using System.Text.Json.Serialization;

namespace Docxtor.Cli.Cli;

internal sealed record ManifestFileModel
{
    [JsonPropertyName("inputs")]
    public List<string> Inputs { get; init; } = [];

    [JsonPropertyName("template")]
    public string? Template { get; init; }

    [JsonPropertyName("output")]
    public string? Output { get; init; }

    [JsonPropertyName("backend")]
    public string? Backend { get; init; }

    [JsonPropertyName("merge")]
    public MergeManifestModel Merge { get; init; } = new();

    [JsonPropertyName("validation")]
    public ValidationManifestModel Validation { get; init; } = new();

    [JsonPropertyName("report")]
    public ReportManifestModel Report { get; init; } = new();
}

internal sealed record MergeManifestModel
{
    [JsonPropertyName("boundary")]
    public string? Boundary { get; init; }

    [JsonPropertyName("preserve_sections")]
    public bool? PreserveSections { get; init; }

    [JsonPropertyName("preserve_headers_footers")]
    public bool? PreserveHeadersFooters { get; init; }

    [JsonPropertyName("numbering")]
    public string? Numbering { get; init; }

    [JsonPropertyName("tracked_changes")]
    public string? TrackedChanges { get; init; }

    [JsonPropertyName("altchunk")]
    public string? AltChunk { get; init; }

    [JsonPropertyName("theme_policy")]
    public string? ThemePolicy { get; init; }

    [JsonPropertyName("external_resources")]
    public string? ExternalResources { get; init; }

    [JsonPropertyName("image_dedup")]
    public bool? ImageDedup { get; init; }

    [JsonPropertyName("update_fields_on_open")]
    public bool? UpdateFieldsOnOpen { get; init; }
}

internal sealed record ValidationManifestModel
{
    [JsonPropertyName("openxml")]
    public bool? OpenXml { get; init; }

    [JsonPropertyName("references")]
    public bool? References { get; init; }

    [JsonPropertyName("visual_qa")]
    public bool? VisualQa { get; init; }

    [JsonPropertyName("emit_report")]
    public bool? EmitReport { get; init; }

    [JsonPropertyName("fail_on_warnings")]
    public bool? FailOnWarnings { get; init; }
}

internal sealed record ReportManifestModel
{
    [JsonPropertyName("path")]
    public string? Path { get; init; }

    [JsonPropertyName("log_format")]
    public string? LogFormat { get; init; }
}
