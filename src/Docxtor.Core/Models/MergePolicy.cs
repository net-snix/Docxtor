namespace Docxtor.Core.Models;

public sealed record MergePolicy
{
    public BoundaryMode BoundaryMode { get; init; } = BoundaryMode.SectionNewPage;

    public SectionPolicy SectionPolicy { get; init; } = SectionPolicy.PreserveSourceSections;

    public bool PreserveHeadersFooters { get; init; } = true;

    public NumberingMode NumberingMode { get; init; } = NumberingMode.PreserveSource;

    public ThemePolicy ThemePolicy { get; init; } = ThemePolicy.BaseWins;

    public TrackedChangesMode TrackedChangesMode { get; init; } = TrackedChangesMode.Fail;

    public AltChunkMode AltChunkMode { get; init; } = AltChunkMode.Reject;

    public ExternalResourceMode ExternalResourceMode { get; init; } = ExternalResourceMode.PreserveLinks;

    public bool ImageDeduplication { get; init; } = true;

    public bool DeterministicIds { get; init; } = true;

    public bool UpdateFieldsOnOpen { get; init; } = true;

    public bool InsertSourceFileTitles { get; init; }
}
