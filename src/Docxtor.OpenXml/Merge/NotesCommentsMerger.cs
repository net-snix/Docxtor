using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Docxtor.OpenXml.Internal;

namespace Docxtor.OpenXml.Merge;

internal sealed class NotesCommentsMerger(
    StyleMerger styleMerger,
    NumberingMerger numberingMerger,
    RelationshipCopier relationshipCopier,
    IdNormalizer idNormalizer)
{
    public void MergeReferencedItems(
        MainDocumentPart sourceMainPart,
        IReadOnlyList<OpenXmlElement> contentRoots,
        MergeContext context)
    {
        MergeFootnotes(sourceMainPart, contentRoots, context);
        MergeEndnotes(sourceMainPart, contentRoots, context);
        MergeComments(sourceMainPart, contentRoots, context);
    }

    private void MergeFootnotes(MainDocumentPart sourceMainPart, IReadOnlyList<OpenXmlElement> contentRoots, MergeContext context)
    {
        if (sourceMainPart.FootnotesPart?.Footnotes is null)
        {
            return;
        }

        var sourceFootnotes = sourceMainPart.FootnotesPart.Footnotes.Elements<Footnote>()
            .Where(item => item.Id?.Value is not null)
            .ToDictionary(item => item.Id!.Value, item => item);
        var destinationPart = OpenXmlPartHelpers.EnsureFootnotesPart(context.MainPart);
        var footnoteIdMap = new Dictionary<long, long>();

        foreach (var footnoteReference in contentRoots.SelectMany(root => root.Descendants<FootnoteReference>()))
        {
            if (footnoteReference.Id?.Value is not long sourceId || sourceId <= 0)
            {
                continue;
            }

            if (!footnoteIdMap.TryGetValue(sourceId, out var destinationId))
            {
                if (!sourceFootnotes.TryGetValue(sourceId, out var sourceFootnote))
                {
                    continue;
                }

                destinationId = context.NextFootnoteId();
                var clonedFootnote = (Footnote)sourceFootnote.CloneNode(true);
                clonedFootnote.Id = destinationId;
                styleMerger.MergeStylesForElements(sourceMainPart, [clonedFootnote], context);
                numberingMerger.MergeNumberingForElements(sourceMainPart, [clonedFootnote], context);
                relationshipCopier.RewriteRelationshipsInElement(clonedFootnote, sourceMainPart.FootnotesPart, destinationPart, context);
                idNormalizer.NormalizeImportedElements([clonedFootnote], context);
                destinationPart.Footnotes!.AppendChild(clonedFootnote);
                footnoteIdMap[sourceId] = destinationId;
                context.RemapSummary.Footnotes++;
            }

            footnoteReference.Id = destinationId;
        }
    }

    private void MergeEndnotes(MainDocumentPart sourceMainPart, IReadOnlyList<OpenXmlElement> contentRoots, MergeContext context)
    {
        if (sourceMainPart.EndnotesPart?.Endnotes is null)
        {
            return;
        }

        var sourceEndnotes = sourceMainPart.EndnotesPart.Endnotes.Elements<Endnote>()
            .Where(item => item.Id?.Value is not null)
            .ToDictionary(item => item.Id!.Value, item => item);
        var destinationPart = OpenXmlPartHelpers.EnsureEndnotesPart(context.MainPart);
        var endnoteIdMap = new Dictionary<long, long>();

        foreach (var endnoteReference in contentRoots.SelectMany(root => root.Descendants<EndnoteReference>()))
        {
            if (endnoteReference.Id?.Value is not long sourceId || sourceId <= 0)
            {
                continue;
            }

            if (!endnoteIdMap.TryGetValue(sourceId, out var destinationId))
            {
                if (!sourceEndnotes.TryGetValue(sourceId, out var sourceEndnote))
                {
                    continue;
                }

                destinationId = context.NextEndnoteId();
                var clonedEndnote = (Endnote)sourceEndnote.CloneNode(true);
                clonedEndnote.Id = destinationId;
                styleMerger.MergeStylesForElements(sourceMainPart, [clonedEndnote], context);
                numberingMerger.MergeNumberingForElements(sourceMainPart, [clonedEndnote], context);
                relationshipCopier.RewriteRelationshipsInElement(clonedEndnote, sourceMainPart.EndnotesPart, destinationPart, context);
                idNormalizer.NormalizeImportedElements([clonedEndnote], context);
                destinationPart.Endnotes!.AppendChild(clonedEndnote);
                endnoteIdMap[sourceId] = destinationId;
                context.RemapSummary.Endnotes++;
            }

            endnoteReference.Id = destinationId;
        }
    }

    private void MergeComments(MainDocumentPart sourceMainPart, IReadOnlyList<OpenXmlElement> contentRoots, MergeContext context)
    {
        if (sourceMainPart.WordprocessingCommentsPart?.Comments is null)
        {
            return;
        }

        var sourceComments = sourceMainPart.WordprocessingCommentsPart.Comments.Elements<Comment>()
            .Where(item => item.Id?.Value is not null)
            .ToDictionary(item => item.Id!.Value!, item => item, StringComparer.Ordinal);
        var destinationPart = OpenXmlPartHelpers.EnsureCommentsPart(context.MainPart);
        var commentIdMap = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var commentReference in contentRoots.SelectMany(root => root.Descendants<CommentReference>()))
        {
            if (commentReference.Id?.Value is not { } sourceId)
            {
                continue;
            }

            if (!commentIdMap.TryGetValue(sourceId, out var destinationId))
            {
                if (!sourceComments.TryGetValue(sourceId, out var sourceComment))
                {
                    continue;
                }

                destinationId = context.NextCommentId().ToString();
                var clonedComment = (Comment)sourceComment.CloneNode(true);
                clonedComment.Id = destinationId;
                styleMerger.MergeStylesForElements(sourceMainPart, [clonedComment], context);
                numberingMerger.MergeNumberingForElements(sourceMainPart, [clonedComment], context);
                relationshipCopier.RewriteRelationshipsInElement(clonedComment, sourceMainPart.WordprocessingCommentsPart, destinationPart, context);
                idNormalizer.NormalizeImportedElements([clonedComment], context);
                destinationPart.Comments!.AppendChild(clonedComment);
                commentIdMap[sourceId] = destinationId;
                context.RemapSummary.Comments++;
            }

            commentReference.Id = destinationId;
        }

        foreach (var rangeStart in contentRoots.SelectMany(root => root.Descendants<CommentRangeStart>()))
        {
            if (rangeStart.Id?.Value is { } sourceId && commentIdMap.TryGetValue(sourceId, out var destinationId))
            {
                rangeStart.Id = destinationId;
            }
        }

        foreach (var rangeEnd in contentRoots.SelectMany(root => root.Descendants<CommentRangeEnd>()))
        {
            if (rangeEnd.Id?.Value is { } sourceId && commentIdMap.TryGetValue(sourceId, out var destinationId))
            {
                rangeEnd.Id = destinationId;
            }
        }
    }

}
