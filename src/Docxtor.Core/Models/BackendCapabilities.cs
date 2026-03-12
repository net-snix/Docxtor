namespace Docxtor.Core.Models;

public sealed record BackendCapabilities
{
    public bool SupportsPreserveSections { get; init; }

    public bool SupportsHeadersFooters { get; init; }

    public bool SupportsNotesComments { get; init; }

    public bool SupportsTextBoxes { get; init; }

    public bool SupportsCharts { get; init; }

    public bool SupportsSmartArt { get; init; }

    public bool SupportsEmbeddedObjects { get; init; }

    public bool SupportsTrackedChangesNormalization { get; init; }

    public bool SupportsAltChunkResolution { get; init; }

    public bool SupportsContinueDestinationNumbering { get; init; }

    public bool SupportsVisualQa { get; init; }

    public IReadOnlySet<BoundaryMode> SupportedBoundaryModes { get; init; } =
        new HashSet<BoundaryMode> { BoundaryMode.SectionNewPage };
}
