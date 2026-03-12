namespace Docxtor.Core.Models;

public enum BoundaryMode
{
    SectionNewPage,
    PageBreak,
    ContinuousSection,
    None,
}

public enum SectionPolicy
{
    PreserveSourceSections,
    UnifyWithBaseHeadersFooters,
}

public enum NumberingMode
{
    PreserveSource,
    ContinueDestination,
}

public enum ThemePolicy
{
    BaseWins,
    ImportFirst,
    TemplateWins,
}

public enum TrackedChangesMode
{
    Fail,
    AcceptAll,
    RejectAll,
}

public enum AltChunkMode
{
    Reject,
    Resolve,
}

public enum ExternalResourceMode
{
    PreserveLinks,
    Materialize,
}

public enum ValidationOutcome
{
    Passed,
    PassedWithWarnings,
    Failed,
    Skipped,
}

public enum FailureCode
{
    None,
    InvalidArguments,
    PreflightCapabilityFailure,
    ValidationFailure,
    OutputWriteFailure,
    CorruptedOrEncryptedInput,
    BackendUnavailable,
    MergeFailure,
}

public enum LogFormat
{
    Text,
    Json,
}
