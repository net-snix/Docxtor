using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Docxtor.Core.Models;
using Docxtor.OpenXml.Internal;
using Docxtor.Validation;

namespace Docxtor.OpenXml.Merge;

internal sealed class OpenXmlMergeExecutor(
    OpenXmlSchemaValidator openXmlSchemaValidator,
    ReferentialIntegrityValidator referentialIntegrityValidator,
    VisualQaValidator visualQaValidator,
    StyleMerger styleMerger,
    NumberingMerger numberingMerger,
    RelationshipCopier relationshipCopier,
    NotesCommentsMerger notesCommentsMerger,
    SectionMerger sectionMerger,
    IdNormalizer idNormalizer)
{
    public async Task<MergeResult> ExecuteAsync(
        MergeJob job,
        PreflightResult preflight,
        IProgress<MergeProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var report = new MergeReport
        {
            CorrelationId = job.CorrelationId,
            Status = "Running",
            StartedAtUtc = DateTimeOffset.UtcNow,
            OutputPath = job.OutputPath,
            Backend = preflight.Backend,
            Policy = job.Policy,
            InputSummaries = job.Inputs,
            PreflightInventories = preflight.Inputs,
            PreflightWarnings = preflight.Warnings,
        };

        var tempOutputPath = Path.Combine(
            Path.GetDirectoryName(job.OutputPath) ?? Directory.GetCurrentDirectory(),
            $".{Path.GetFileName(job.OutputPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            var orderedInputs = job.Inputs.OrderBy(item => item.SourceIndex).ToArray();
            var baseInput = job.TemplatePath is null ? orderedInputs.First() : null;
            var baseDocumentPath = job.TemplatePath ?? baseInput!.PathOrId;
            Directory.CreateDirectory(Path.GetDirectoryName(tempOutputPath) ?? Directory.GetCurrentDirectory());
            File.Copy(baseDocumentPath, tempOutputPath, overwrite: true);

            if (baseInput is not null)
            {
                progress?.Report(new MergeProgressUpdate
                {
                    Stage = MergeStage.MergingInput,
                    CurrentInputIndex = baseInput.SourceIndex + 1,
                    TotalInputs = job.Inputs.Count,
                    InputDisplayName = baseInput.DisplayName,
                });
            }

            using (var destinationDocument = WordprocessingDocument.Open(tempOutputPath, true))
            {
                var mainPart = destinationDocument.MainDocumentPart
                    ?? throw new InvalidOperationException("The base document is missing a main document part.");
                mainPart.Document ??= new Document(new Body());
                mainPart.Document.Body ??= new Body();

                if (job.Policy.InsertSourceFileTitles && baseInput is not null)
                {
                    mainPart.Document.Body.InsertAt(CreateSourceFileTitleParagraph(baseInput.DisplayName), 0);
                }

                var context = MergeContext.Create(mainPart, job.Policy);
                var sourceInputs = job.TemplatePath is null
                    ? orderedInputs.Skip(1).ToArray()
                    : orderedInputs;

                foreach (var input in sourceInputs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report(new MergeProgressUpdate
                    {
                        Stage = MergeStage.MergingInput,
                        CurrentInputIndex = input.SourceIndex + 1,
                        TotalInputs = job.Inputs.Count,
                        InputDisplayName = input.DisplayName,
                    });
                    MergeSourceDocument(input, destinationDocument, context);
                }

                if (job.Policy.UpdateFieldsOnOpen)
                {
                    var settings = OpenXmlPartHelpers.EnsureSettingsPart(mainPart).Settings!;
                    settings.RemoveAllChildren<UpdateFieldsOnOpen>();
                    settings.AddChild(new UpdateFieldsOnOpen { Val = true }, true);
                    settings.Save();
                }

                mainPart.Document.Save();
                report.MergeWarnings = context.Warnings.ToArray();
                report.RemapCounts.RelationshipIds = context.RemapSummary.RelationshipIds;
                report.RemapCounts.Styles = context.RemapSummary.Styles;
                report.RemapCounts.Numbering = context.RemapSummary.Numbering;
                report.RemapCounts.AbstractNumbering = context.RemapSummary.AbstractNumbering;
                report.RemapCounts.Footnotes = context.RemapSummary.Footnotes;
                report.RemapCounts.Endnotes = context.RemapSummary.Endnotes;
                report.RemapCounts.Comments = context.RemapSummary.Comments;
                report.RemapCounts.BookmarkIds = context.RemapSummary.BookmarkIds;
                report.RemapCounts.BookmarkNames = context.RemapSummary.BookmarkNames;
                report.RemapCounts.DrawingIds = context.RemapSummary.DrawingIds;
                report.RemapCounts.PictureIds = context.RemapSummary.PictureIds;
                report.RemapCounts.HeaderFooterParts = context.RemapSummary.HeaderFooterParts;
                report.RemapCounts.ImagesDeduplicated = context.RemapSummary.ImagesDeduplicated;
            }

            report.OpenXmlValidation = job.Validation.RunOpenXmlValidation
                ? await openXmlSchemaValidator.ValidateAsync(tempOutputPath, cancellationToken)
                : Skipped("openxml-validation-skipped", "Open XML schema validation was disabled.");
            report.ReferentialIntegrityValidation = job.Validation.RunReferentialIntegrityChecks
                ? await referentialIntegrityValidator.ValidateAsync(tempOutputPath, cancellationToken)
                : Skipped("reference-validation-skipped", "Referential-integrity validation was disabled.");
            report.VisualQaValidation = job.Validation.RunVisualRegression
                ? await visualQaValidator.ValidateAsync(tempOutputPath, cancellationToken)
                : Skipped("visual-qa-skipped", "Visual QA was disabled.");

            var validationFailed = report.OpenXmlValidation.Outcome == ValidationOutcome.Failed ||
                report.ReferentialIntegrityValidation.Outcome == ValidationOutcome.Failed ||
                report.VisualQaValidation.Outcome == ValidationOutcome.Failed;
            if (validationFailed)
            {
                report.Status = "Failed";
                report.FailureCode = FailureCode.ValidationFailure;
                report.Errors = report.OpenXmlValidation.Messages
                    .Concat(report.ReferentialIntegrityValidation.Messages)
                    .Concat(report.VisualQaValidation.Messages)
                    .ToArray();
                SafeDelete(tempOutputPath);
                FinalizeReport(report, null);
                return new MergeResult
                {
                    Success = false,
                    FailureCode = FailureCode.ValidationFailure,
                    Report = report,
                };
            }

            File.Move(tempOutputPath, job.OutputPath, overwrite: true);
            FinalizeReport(report, job.OutputPath);

            return new MergeResult
            {
                Success = true,
                OutputPath = job.OutputPath,
                FailureCode = FailureCode.None,
                Report = report,
            };
        }
        catch (Exception ex)
        {
            SafeDelete(tempOutputPath);
            report.Status = "Failed";
            report.FailureCode = FailureCode.MergeFailure;
            report.Errors =
            [
                new DiagnosticMessage
                {
                    Code = "merge-failed",
                    Message = ex.Message,
                },
            ];
            FinalizeReport(report, null);

            return new MergeResult
            {
                Success = false,
                FailureCode = FailureCode.MergeFailure,
                Report = report,
            };
        }
    }

    private void MergeSourceDocument(
        InputDocument input,
        WordprocessingDocument destinationDocument,
        MergeContext context)
    {
        using var sourceDocument = WordprocessingDocument.Open(input.PathOrId, false);
        var sourceMainPart = sourceDocument.MainDocumentPart
            ?? throw new InvalidOperationException($"Source document '{input.PathOrId}' is missing a main document part.");
        sourceMainPart.Document ??= new Document(new Body());
        sourceMainPart.Document.Body ??= new Body();

        if (context.MainPart.ThemePart?.Theme?.OuterXml is { } destinationTheme &&
            sourceMainPart.ThemePart?.Theme?.OuterXml is { } sourceTheme &&
            !StringComparer.Ordinal.Equals(destinationTheme, sourceTheme))
        {
            context.AddWarning(
                "theme-conflict",
                "An imported document uses a different theme. Base-document theme wins.",
                input.PathOrId);
        }

        var importedElements = sourceMainPart.Document.Body.ChildElements
            .Where(element => element is not SectionProperties)
            .Select(element => element.CloneNode(true))
            .ToList();

        if (context.Policy.InsertSourceFileTitles)
        {
            importedElements.Insert(0, CreateSourceFileTitleParagraph(input.DisplayName));
        }

        var sourceFinalSectionProperties = sourceMainPart.Document.Body.Elements<SectionProperties>().LastOrDefault();

        styleMerger.MergeStylesForElements(sourceMainPart, importedElements, context);
        numberingMerger.MergeNumberingForElements(sourceMainPart, importedElements, context);
        notesCommentsMerger.MergeReferencedItems(sourceMainPart, importedElements, context);

        foreach (var importedElement in importedElements)
        {
            relationshipCopier.RewriteRelationshipsInElement(importedElement, sourceMainPart, context.MainPart, context);
        }

        idNormalizer.NormalizeImportedElements(importedElements, context);
        sectionMerger.AppendContent(sourceMainPart, importedElements, sourceFinalSectionProperties, context);
    }

    private static Paragraph CreateSourceFileTitleParagraph(string displayName)
    {
        var titleText = Path.GetFileNameWithoutExtension(displayName);
        if (string.IsNullOrWhiteSpace(titleText))
        {
            titleText = displayName;
        }

        return new Paragraph(
            new ParagraphProperties(
                new KeepNext(),
                new SpacingBetweenLines
                {
                    Before = "240",
                    After = "120",
                },
                new OutlineLevel
                {
                    Val = 0,
                }),
            new Run(
                new RunProperties(
                    new Bold(),
                    new FontSize
                    {
                        Val = "32",
                    }),
                new Text(titleText)
                {
                    Space = SpaceProcessingModeValues.Preserve,
                }));
    }

    private static ValidationSummary Skipped(string code, string message)
    {
        return new ValidationSummary
        {
            Outcome = ValidationOutcome.Skipped,
            Messages =
            [
                new DiagnosticMessage
                {
                    Code = code,
                    Message = message,
                },
            ],
        };
    }

    private static void FinalizeReport(MergeReport report, string? outputPath)
    {
        report.FinishedAtUtc = DateTimeOffset.UtcNow;
        report.DurationMs = (long)(report.FinishedAtUtc.Value - report.StartedAtUtc).TotalMilliseconds;
        report.Status = report.FailureCode == FailureCode.None ? "Success" : "Failed";

        if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
        {
            report.OutputSizeBytes = new FileInfo(outputPath).Length;
        }
    }

    private static void SafeDelete(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
