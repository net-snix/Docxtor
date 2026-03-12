using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Docxtor.OpenXml.Internal;

namespace Docxtor.OpenXml.Merge;

internal sealed class IdNormalizer
{
    public void NormalizeImportedElements(IEnumerable<OpenXmlElement> contentRoots, MergeContext context)
    {
        foreach (var root in contentRoots)
        {
            NormalizeBookmarks(root, context);
            NormalizeDrawingIds(root, context);
        }
    }

    private static void NormalizeBookmarks(OpenXmlElement root, MergeContext context)
    {
        var bookmarkIdMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var bookmarkNameMap = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var bookmarkStart in root.Descendants<BookmarkStart>())
        {
            var oldId = bookmarkStart.Id?.Value ?? Guid.NewGuid().ToString("N");
            var newId = context.NextBookmarkId().ToString();
            bookmarkIdMap[oldId] = newId;
            bookmarkStart.Id = newId;
            context.RemapSummary.BookmarkIds++;

            if (bookmarkStart.Name?.Value is { } bookmarkName)
            {
                var resolvedName = GetUniqueBookmarkName(bookmarkName, context);
                if (!StringComparer.Ordinal.Equals(bookmarkName, resolvedName))
                {
                    bookmarkNameMap[bookmarkName] = resolvedName;
                    context.RemapSummary.BookmarkNames++;
                }

                bookmarkStart.Name = resolvedName;
            }
        }

        foreach (var bookmarkEnd in root.Descendants<BookmarkEnd>())
        {
            if (bookmarkEnd.Id?.Value is { } oldId && bookmarkIdMap.TryGetValue(oldId, out var mappedId))
            {
                bookmarkEnd.Id = mappedId;
            }
        }

        foreach (var hyperlink in root.Descendants<Hyperlink>())
        {
            if (hyperlink.Anchor?.Value is { } anchor && bookmarkNameMap.TryGetValue(anchor, out var mappedAnchor))
            {
                hyperlink.Anchor = mappedAnchor;
            }
        }
    }

    private static void NormalizeDrawingIds(OpenXmlElement root, MergeContext context)
    {
        foreach (var docProperties in root.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>())
        {
            docProperties.Id = context.NextDocPropertiesId();
            context.RemapSummary.DrawingIds++;
        }

        foreach (var nonVisualDrawingProperties in root.Descendants<DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties>())
        {
            nonVisualDrawingProperties.Id = context.NextPictureId();
            context.RemapSummary.PictureIds++;
        }
    }

    private static string GetUniqueBookmarkName(string baseName, MergeContext context)
    {
        var candidate = baseName;
        var suffix = 1;
        while (!context.BookmarkNames.Add(candidate))
        {
            candidate = $"{baseName}_{suffix++}";
        }

        return candidate;
    }
}
