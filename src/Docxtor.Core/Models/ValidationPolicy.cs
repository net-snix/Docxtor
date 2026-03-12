namespace Docxtor.Core.Models;

public sealed record ValidationPolicy
{
    public bool RunOpenXmlValidation { get; init; } = true;

    public bool RunReferentialIntegrityChecks { get; init; } = true;

    public bool RunVisualRegression { get; init; }

    public bool EmitReport { get; init; } = true;

    public bool FailOnWarnings { get; init; }
}
