using Docxtor.Core.Models;

namespace Docxtor.Validation;

public sealed class VisualQaValidator
{
    public Task<ValidationSummary> ValidateAsync(string documentPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new ValidationSummary
        {
            Outcome = ValidationOutcome.Skipped,
            Messages =
            [
                new DiagnosticMessage
                {
                    Code = "visual-qa-skipped",
                    Message = $"Visual QA is not configured in this environment for '{documentPath}'.",
                },
            ],
        });
    }
}
