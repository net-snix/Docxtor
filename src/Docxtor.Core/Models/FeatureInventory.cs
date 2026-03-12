namespace Docxtor.Core.Models;

public sealed record FeatureInventory
{
    public required string InputPath { get; init; }

    public bool HasHeaders { get; init; }

    public bool HasFooters { get; init; }

    public bool HasFootnotes { get; init; }

    public bool HasEndnotes { get; init; }

    public bool HasComments { get; init; }

    public bool HasTrackedChanges { get; init; }

    public bool HasCharts { get; init; }

    public bool HasSmartArt { get; init; }

    public bool HasEmbeddedObjects { get; init; }

    public bool HasTextBoxes { get; init; }

    public bool HasExternalHyperlinks { get; init; }

    public bool HasExternalImages { get; init; }

    public bool HasBookmarks { get; init; }

    public bool HasNumbering { get; init; }

    public bool HasStyles { get; init; }

    public bool HasStylesWithEffects { get; init; }

    public bool HasTheme { get; init; }

    public bool HasAltChunk { get; init; }

    public bool HasFields { get; init; }

    public bool HasContentControls { get; init; }

    public IReadOnlyDictionary<string, int> PartCounts { get; init; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> RelationshipCounts { get; init; } = new Dictionary<string, int>();
}
