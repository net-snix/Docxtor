using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Docxtor.Core.Models;
using Docxtor.OpenXml.Internal;

namespace Docxtor.OpenXml.Internal;

internal sealed class MergeContext
{
    private int _nextAbstractNumberingId;
    private int _nextCommentId;
    private uint _nextDocPropertiesId;
    private int _nextEndnoteId;
    private int _nextFootnoteId;
    private uint _nextPictureId;
    private uint _nextBookmarkId;
    private int _nextNumberingId;

    private MergeContext(MainDocumentPart mainPart, MergePolicy policy)
    {
        MainPart = mainPart;
        Policy = policy;
    }

    public MainDocumentPart MainPart { get; }

    public MergePolicy Policy { get; }

    public RemapSummary RemapSummary { get; } = new();

    public List<DiagnosticMessage> Warnings { get; } = [];

    public Dictionary<string, string> RelationshipIdMap { get; } = new(StringComparer.Ordinal);

    public Dictionary<OpenXmlPartContainer, RelationshipLookup> SourceRelationshipLookups { get; } =
        new(ReferenceEqualityComparer.Instance);

    public Dictionary<string, ImagePart> ImagePartsByHash { get; } = new(StringComparer.Ordinal);

    public HashSet<string> BookmarkNames { get; } = new(StringComparer.Ordinal);

    public static MergeContext Create(MainDocumentPart mainPart, MergePolicy policy)
    {
        var context = new MergeContext(mainPart, policy);
        uint nextBookmarkId = 0;
        uint nextDocPropertiesId = 0;
        uint nextPictureId = 0;

        foreach (var root in OpenXmlPartHelpers.EnumerateRootElements(mainPart))
        {
            foreach (var bookmarkStart in root.Descendants<BookmarkStart>())
            {
                if (uint.TryParse(bookmarkStart.Id?.Value, out var bookmarkId) && bookmarkId > nextBookmarkId)
                {
                    nextBookmarkId = bookmarkId;
                }

                if (!string.IsNullOrWhiteSpace(bookmarkStart.Name?.Value))
                {
                    context.BookmarkNames.Add(bookmarkStart.Name!.Value!);
                }
            }

            foreach (var docProperties in root.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>())
            {
                var docPropertiesId = docProperties.Id?.Value ?? 0U;
                if (docPropertiesId > nextDocPropertiesId)
                {
                    nextDocPropertiesId = docPropertiesId;
                }
            }

            foreach (var pictureProperties in root.Descendants<DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties>())
            {
                var pictureId = pictureProperties.Id?.Value ?? 0U;
                if (pictureId > nextPictureId)
                {
                    nextPictureId = pictureId;
                }
            }
        }

        context._nextBookmarkId = nextBookmarkId + 1;
        context._nextDocPropertiesId = nextDocPropertiesId + 1;
        context._nextPictureId = nextPictureId + 1;
        context._nextFootnoteId = OpenXmlPartHelpers.NextIntId(
            mainPart.FootnotesPart?.Footnotes?.Elements<Footnote>().Select(item => item.Id?.Value.ToString()) ?? []);
        context._nextEndnoteId = OpenXmlPartHelpers.NextIntId(
            mainPart.EndnotesPart?.Endnotes?.Elements<Endnote>().Select(item => item.Id?.Value.ToString()) ?? []);
        context._nextCommentId = OpenXmlPartHelpers.NextIntId(
            mainPart.WordprocessingCommentsPart?.Comments?.Elements<Comment>().Select(item => item.Id?.Value) ?? []);
        context._nextNumberingId = mainPart.NumberingDefinitionsPart?.Numbering?
            .Elements<NumberingInstance>()
            .Select(item => item.NumberID?.Value ?? 0)
            .DefaultIfEmpty()
            .Max() + 1 ?? 1;
        context._nextAbstractNumberingId = mainPart.NumberingDefinitionsPart?.Numbering?
            .Elements<AbstractNum>()
            .Select(item => item.AbstractNumberId?.Value ?? 0)
            .DefaultIfEmpty()
            .Max() + 1 ?? 1;

        foreach (var imagePart in OpenXmlPartHelpers.EnumerateParts(mainPart).OfType<ImagePart>())
        {
            var hash = OpenXmlPartHelpers.ComputeHash(imagePart);
            context.ImagePartsByHash.TryAdd(hash, imagePart);
        }

        return context;
    }

    public uint NextBookmarkId() => _nextBookmarkId++;

    public uint NextDocPropertiesId() => _nextDocPropertiesId++;

    public uint NextPictureId() => _nextPictureId++;

    public int NextFootnoteId() => _nextFootnoteId++;

    public int NextEndnoteId() => _nextEndnoteId++;

    public int NextCommentId() => _nextCommentId++;

    public int NextNumberingId() => _nextNumberingId++;

    public int NextAbstractNumberingId() => _nextAbstractNumberingId++;

    public void AddWarning(string code, string message, string? inputPath = null)
    {
        Warnings.Add(new DiagnosticMessage
        {
            Code = code,
            Message = message,
            InputPath = inputPath,
        });
    }

    internal sealed class RelationshipLookup
    {
        private RelationshipLookup(
            Dictionary<string, OpenXmlPart> internalPartsById,
            Dictionary<string, ExternalRelationship> externalRelationshipsById,
            Dictionary<string, HyperlinkRelationship> hyperlinkRelationshipsById)
        {
            InternalPartsById = internalPartsById;
            ExternalRelationshipsById = externalRelationshipsById;
            HyperlinkRelationshipsById = hyperlinkRelationshipsById;
        }

        public Dictionary<string, OpenXmlPart> InternalPartsById { get; }

        public Dictionary<string, ExternalRelationship> ExternalRelationshipsById { get; }

        public Dictionary<string, HyperlinkRelationship> HyperlinkRelationshipsById { get; }

        public static RelationshipLookup Create(OpenXmlPartContainer owner)
        {
            var internalPartsById = owner.Parts
                .Where(item => !string.IsNullOrWhiteSpace(item.RelationshipId))
                .ToDictionary(item => item.RelationshipId!, item => item.OpenXmlPart, StringComparer.Ordinal);
            var externalRelationshipsById = owner.ExternalRelationships
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .ToDictionary(item => item.Id!, item => item, StringComparer.Ordinal);
            var hyperlinkRelationshipsById = owner is OpenXmlPart part
                ? part.HyperlinkRelationships
                    .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                    .ToDictionary(item => item.Id!, item => item, StringComparer.Ordinal)
                : new Dictionary<string, HyperlinkRelationship>(StringComparer.Ordinal);

            return new RelationshipLookup(
                internalPartsById,
                externalRelationshipsById,
                hyperlinkRelationshipsById);
        }
    }
}
