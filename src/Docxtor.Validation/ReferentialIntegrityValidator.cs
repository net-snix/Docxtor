using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Docxtor.Core.Models;

namespace Docxtor.Validation;

public sealed class ReferentialIntegrityValidator
{
    private const string RelationshipsNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string RelationshipsNamespaceStrict = "http://purl.oclc.org/ooxml/officeDocument/relationships";

    public Task<ValidationSummary> ValidateAsync(string documentPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var messages = new List<DiagnosticMessage>();

        using var document = WordprocessingDocument.Open(documentPath, false);
        var mainPart = document.MainDocumentPart;
        if (mainPart?.Document?.Body is null)
        {
            messages.Add(new DiagnosticMessage
            {
                Code = "missing-main-document",
                Message = "The output document is missing a main document body.",
            });

            return Task.FromResult(BuildSummary(messages));
        }

        ValidateBodySectionProperties(mainPart.Document.Body, messages);
        ValidateRelationships(mainPart, messages);
        ValidateRelatedPartRelationships(mainPart, messages);
        ValidateStyleReferences(mainPart, messages);
        ValidateNumbering(mainPart, messages);
        ValidateNoteReferences(mainPart, messages);
        ValidateUniqueIds(mainPart, messages);

        return Task.FromResult(BuildSummary(messages));
    }

    private static ValidationSummary BuildSummary(List<DiagnosticMessage> messages)
    {
        return new ValidationSummary
        {
            Outcome = messages.Count == 0 ? ValidationOutcome.Passed : ValidationOutcome.Failed,
            Messages = messages,
        };
    }

    private static void ValidateBodySectionProperties(Body body, List<DiagnosticMessage> messages)
    {
        var bodySectionProperties = body.Elements<SectionProperties>().ToArray();
        if (bodySectionProperties.Length != 1 || body.LastChild is not SectionProperties)
        {
            messages.Add(new DiagnosticMessage
            {
                Code = "body-sectpr",
                Message = "The document body must end with exactly one body-level section properties element.",
            });
        }
    }

    private static void ValidateRelationships(OpenXmlPart part, List<DiagnosticMessage> messages)
    {
        var relationshipIds = ReadRelationshipIds(part.RootElement).Distinct(StringComparer.Ordinal);
        foreach (var relationshipId in relationshipIds)
        {
            var hasInternal = part.Parts.Any(child => child.RelationshipId == relationshipId);
            var hasExternal = part.ExternalRelationships.Any(child => child.Id == relationshipId);
            var hasHyperlink = part.HyperlinkRelationships.Any(child => child.Id == relationshipId);

            if (!hasInternal && !hasExternal && !hasHyperlink)
            {
                messages.Add(new DiagnosticMessage
                {
                    Code = "dangling-relationship",
                    Message = $"Relationship '{relationshipId}' could not be resolved in part '{part.Uri}'.",
                    PartUri = part.Uri.ToString(),
                });
            }
        }
    }

    private static void ValidateRelatedPartRelationships(MainDocumentPart mainPart, List<DiagnosticMessage> messages)
    {
        foreach (var headerPart in mainPart.HeaderParts)
        {
            ValidateRelationships(headerPart, messages);
        }

        foreach (var footerPart in mainPart.FooterParts)
        {
            ValidateRelationships(footerPart, messages);
        }

        if (mainPart.FootnotesPart is not null)
        {
            ValidateRelationships(mainPart.FootnotesPart, messages);
        }

        if (mainPart.EndnotesPart is not null)
        {
            ValidateRelationships(mainPart.EndnotesPart, messages);
        }

        if (mainPart.WordprocessingCommentsPart is not null)
        {
            ValidateRelationships(mainPart.WordprocessingCommentsPart, messages);
        }
    }

    private static void ValidateStyleReferences(MainDocumentPart mainPart, List<DiagnosticMessage> messages)
    {
        var styles = mainPart.StyleDefinitionsPart?.Styles?
            .Elements<Style>()
            .Select(style => style.StyleId?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal)
            ?? [];

        if (styles.Count == 0)
        {
            return;
        }

        var body = mainPart.Document?.Body;
        if (body is null)
        {
            return;
        }

        foreach (var styleId in ReadStyleIds(body))
        {
            if (!styles.Contains(styleId))
            {
                messages.Add(new DiagnosticMessage
                {
                    Code = "missing-style",
                    Message = $"Referenced style '{styleId}' is missing from styles.xml.",
                    PartUri = mainPart.StyleDefinitionsPart?.Uri.ToString(),
                });
            }
        }
    }

    private static void ValidateNumbering(MainDocumentPart mainPart, List<DiagnosticMessage> messages)
    {
        var numberingPart = mainPart.NumberingDefinitionsPart;
        var numberingIds = numberingPart?.Numbering?
            .Elements<NumberingInstance>()
            .Select(instance => instance.NumberID?.Value)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToHashSet()
            ?? [];

        var body = mainPart.Document?.Body;
        if (body is null)
        {
            return;
        }

        foreach (var numId in body
                     .Descendants<NumberingId>()
                     .Select(item => item.Val?.Value)
                     .Where(value => value.HasValue)
                     .Select(value => value!.Value))
        {
            if (!numberingIds.Contains(numId))
            {
                messages.Add(new DiagnosticMessage
                {
                    Code = "missing-numbering",
                    Message = $"Numbering id '{numId}' is referenced but not defined.",
                    PartUri = numberingPart?.Uri.ToString(),
                });
            }
        }
    }

    private static void ValidateNoteReferences(MainDocumentPart mainPart, List<DiagnosticMessage> messages)
    {
        var body = mainPart.Document?.Body;
        if (body is null)
        {
            return;
        }

        ValidateNoteReferences(
            "footnote",
            body.Descendants<FootnoteReference>().Select(item => item.Id?.Value.ToString()),
            mainPart.FootnotesPart?.Footnotes?.Elements<Footnote>().Select(item => item.Id?.Value.ToString()),
            messages);

        ValidateNoteReferences(
            "endnote",
            body.Descendants<EndnoteReference>().Select(item => item.Id?.Value.ToString()),
            mainPart.EndnotesPart?.Endnotes?.Elements<Endnote>().Select(item => item.Id?.Value.ToString()),
            messages);

        ValidateNoteReferences(
            "comment",
            body.Descendants<CommentReference>().Select(item => item.Id?.Value),
            mainPart.WordprocessingCommentsPart?.Comments?.Elements<Comment>().Select(item => item.Id?.Value),
            messages);
    }

    private static void ValidateNoteReferences(
        string label,
        IEnumerable<string?> references,
        IEnumerable<string?>? definitions,
        List<DiagnosticMessage> messages)
    {
        var definitionIds = definitions?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal)
            ?? [];

        foreach (var reference in references.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal))
        {
            if (reference is not null && !definitionIds.Contains(reference))
            {
                messages.Add(new DiagnosticMessage
                {
                    Code = $"missing-{label}",
                    Message = $"{label} '{reference}' is referenced but not defined.",
                });
            }
        }
    }

    private static void ValidateUniqueIds(MainDocumentPart mainPart, List<DiagnosticMessage> messages)
    {
        var body = mainPart.Document?.Body;
        if (body is null)
        {
            return;
        }

        ValidateDuplicates(
            "duplicate-bookmark-id",
            body.Descendants<BookmarkStart>().Select(item => item.Id?.Value),
            messages);

        ValidateDuplicates(
            "duplicate-bookmark-name",
            body.Descendants<BookmarkStart>().Select(item => item.Name?.Value),
            messages);

        ValidateDuplicates(
            "duplicate-comment-id",
            mainPart.WordprocessingCommentsPart?.Comments?.Elements<Comment>().Select(item => item.Id?.Value) ?? [],
            messages);

        ValidateDuplicates(
            "duplicate-footnote-id",
            mainPart.FootnotesPart?.Footnotes?.Elements<Footnote>().Select(item => item.Id?.Value.ToString()) ?? [],
            messages);

        ValidateDuplicates(
            "duplicate-endnote-id",
            mainPart.EndnotesPart?.Endnotes?.Elements<Endnote>().Select(item => item.Id?.Value.ToString()) ?? [],
            messages);
    }

    private static void ValidateDuplicates(
        string code,
        IEnumerable<string?> values,
        List<DiagnosticMessage> messages)
    {
        var duplicates = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (var duplicate in duplicates)
        {
            messages.Add(new DiagnosticMessage
            {
                Code = code,
                Message = $"Duplicate identifier '{duplicate}' detected.",
            });
        }
    }

    private static IEnumerable<string> ReadStyleIds(OpenXmlElement root)
    {
        foreach (var paragraphStyle in root.Descendants<ParagraphStyleId>())
        {
            if (!string.IsNullOrWhiteSpace(paragraphStyle.Val?.Value))
            {
                yield return paragraphStyle.Val!.Value!;
            }
        }

        foreach (var runStyle in root.Descendants<RunStyle>())
        {
            if (!string.IsNullOrWhiteSpace(runStyle.Val?.Value))
            {
                yield return runStyle.Val!.Value!;
            }
        }

        foreach (var tableStyle in root.Descendants<TableStyle>())
        {
            if (!string.IsNullOrWhiteSpace(tableStyle.Val?.Value))
            {
                yield return tableStyle.Val!.Value!;
            }
        }
    }

    private static IEnumerable<string> ReadRelationshipIds(OpenXmlElement? root)
    {
        if (root is null)
        {
            yield break;
        }

        foreach (var element in EnumerateSelfAndDescendants(root))
        {
            foreach (var attribute in element.GetAttributes())
            {
                if ((attribute.NamespaceUri == RelationshipsNamespace ||
                     attribute.NamespaceUri == RelationshipsNamespaceStrict) &&
                    !string.IsNullOrWhiteSpace(attribute.Value))
                {
                    yield return attribute.Value;
                }
            }
        }
    }

    private static IEnumerable<OpenXmlElement> EnumerateSelfAndDescendants(OpenXmlElement root)
    {
        yield return root;

        foreach (var descendant in root.Descendants())
        {
            yield return descendant;
        }
    }
}
