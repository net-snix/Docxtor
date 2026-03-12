using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml;
using Docxtor.Core.Models;

namespace Docxtor.Validation;

public sealed class OpenXmlSchemaValidator
{
    private const FileFormatVersions ValidationTargetVersion = FileFormatVersions.Office2013;

    public Task<ValidationSummary> ValidateAsync(string documentPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var document = WordprocessingDocument.Open(documentPath, false);
        var validator = new OpenXmlValidator(ValidationTargetVersion);
        var messages = validator
            .Validate(document)
            .Take(100)
            .Select(error => new DiagnosticMessage
            {
                Code = "openxml-schema",
                Message = error.Description ?? string.Empty,
                PartUri = error.Path?.PartUri?.ToString(),
            })
            .ToArray();

        return Task.FromResult(new ValidationSummary
        {
            Outcome = messages.Length == 0 ? ValidationOutcome.Passed : ValidationOutcome.Failed,
            Messages = messages,
        });
    }
}
