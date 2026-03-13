using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Docxtor.OpenXml.Internal;

namespace Docxtor.OpenXml.Merge;

internal sealed class StyleMerger
{
    public void MergeStylesForElements(
        MainDocumentPart sourceMainPart,
        IReadOnlyList<OpenXmlElement> contentRoots,
        MergeContext context)
    {
        if (contentRoots.Count == 0 || sourceMainPart.StyleDefinitionsPart?.Styles is null)
        {
            return;
        }

        var destinationStylesPart = OpenXmlPartHelpers.EnsureStylesPart(context.MainPart);
        var destinationStyles = destinationStylesPart.Styles!;
        var sourceStyles = sourceMainPart.StyleDefinitionsPart.Styles.Elements<Style>()
            .Where(style => !string.IsNullOrWhiteSpace(style.StyleId))
            .ToDictionary(style => style.StyleId!.Value!, style => style, StringComparer.Ordinal);
        var destinationById = destinationStyles.Elements<Style>()
            .Where(style => !string.IsNullOrWhiteSpace(style.StyleId))
            .ToDictionary(style => style.StyleId!.Value!, style => style, StringComparer.Ordinal);
        var reservedStyleIds = destinationById.Keys.ToHashSet(StringComparer.Ordinal);
        var normalizedStyleXml = new Dictionary<Style, string>(ReferenceEqualityComparer.Instance);

        var directStyleIds = CollectDirectStyleIds(contentRoots);
        var usedStyleIds = ExpandUsedStyles(sourceStyles, directStyleIds);
        if (usedStyleIds.Count == 0)
        {
            return;
        }

        var styleIdMap = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var sourceStyleId in usedStyleIds)
        {
            if (!sourceStyles.TryGetValue(sourceStyleId, out var sourceStyle))
            {
                continue;
            }

            if (destinationById.TryGetValue(sourceStyleId, out var destinationStyle))
            {
                styleIdMap[sourceStyleId] = AreEquivalent(sourceStyle, destinationStyle, normalizedStyleXml)
                    ? sourceStyleId
                    : GenerateUniqueStyleId(sourceStyleId, reservedStyleIds);
            }
            else
            {
                styleIdMap[sourceStyleId] = sourceStyleId;
            }

            reservedStyleIds.Add(styleIdMap[sourceStyleId]);
        }

        foreach (var sourceStyleId in usedStyleIds)
        {
            if (!sourceStyles.TryGetValue(sourceStyleId, out var sourceStyle))
            {
                continue;
            }

            var destinationStyleId = styleIdMap[sourceStyleId];
            if (destinationById.TryGetValue(destinationStyleId, out var existingStyle) &&
                AreEquivalent(sourceStyle, existingStyle, normalizedStyleXml))
            {
                continue;
            }

            var clonedStyle = (Style)sourceStyle.CloneNode(true);
            clonedStyle.StyleId = destinationStyleId;
            RewriteStyleReferences(clonedStyle, styleIdMap);
            destinationStyles.AppendChild(clonedStyle);
            destinationById[destinationStyleId] = clonedStyle;
            context.RemapSummary.Styles++;
        }

        RewriteContentStyleReferences(contentRoots, styleIdMap);
        MergeStylesWithEffects(sourceMainPart, usedStyleIds, styleIdMap, context, normalizedStyleXml);
    }

    private static HashSet<string> CollectDirectStyleIds(IEnumerable<OpenXmlElement> roots)
    {
        var styleIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var root in roots)
        {
            foreach (var styleId in root.Descendants<ParagraphStyleId>().Select(item => item.Val?.Value))
            {
                if (!string.IsNullOrWhiteSpace(styleId))
                {
                    styleIds.Add(styleId!);
                }
            }

            foreach (var styleId in root.Descendants<RunStyle>().Select(item => item.Val?.Value))
            {
                if (!string.IsNullOrWhiteSpace(styleId))
                {
                    styleIds.Add(styleId!);
                }
            }

            foreach (var styleId in root.Descendants<TableStyle>().Select(item => item.Val?.Value))
            {
                if (!string.IsNullOrWhiteSpace(styleId))
                {
                    styleIds.Add(styleId!);
                }
            }
        }

        return styleIds;
    }

    private static HashSet<string> ExpandUsedStyles(
        IReadOnlyDictionary<string, Style> sourceStyles,
        IEnumerable<string> directStyleIds)
    {
        var pending = new Queue<string>(directStyleIds.Where(sourceStyles.ContainsKey));
        var discovered = new HashSet<string>(StringComparer.Ordinal);

        while (pending.Count > 0)
        {
            var styleId = pending.Dequeue();
            if (!discovered.Add(styleId))
            {
                continue;
            }

            if (!sourceStyles.TryGetValue(styleId, out var style))
            {
                continue;
            }

            EnqueueStyleReference(style.BasedOn?.Val?.Value, sourceStyles, discovered, pending);
            EnqueueStyleReference(style.NextParagraphStyle?.Val?.Value, sourceStyles, discovered, pending);
            EnqueueStyleReference(style.LinkedStyle?.Val?.Value, sourceStyles, discovered, pending);
        }

        return discovered;
    }

    private static void EnqueueStyleReference(
        string? styleId,
        IReadOnlyDictionary<string, Style> sourceStyles,
        IReadOnlySet<string> discovered,
        Queue<string> pending)
    {
        if (!string.IsNullOrWhiteSpace(styleId) && sourceStyles.ContainsKey(styleId) && !discovered.Contains(styleId))
        {
            pending.Enqueue(styleId);
        }
    }

    private static void RewriteContentStyleReferences(IEnumerable<OpenXmlElement> roots, IReadOnlyDictionary<string, string> styleIdMap)
    {
        foreach (var root in roots)
        {
            foreach (var style in root.Descendants<ParagraphStyleId>())
            {
                if (style.Val?.Value is { } value && styleIdMap.TryGetValue(value, out var mapped))
                {
                    style.Val = mapped;
                }
            }

            foreach (var style in root.Descendants<RunStyle>())
            {
                if (style.Val?.Value is { } value && styleIdMap.TryGetValue(value, out var mapped))
                {
                    style.Val = mapped;
                }
            }

            foreach (var style in root.Descendants<TableStyle>())
            {
                if (style.Val?.Value is { } value && styleIdMap.TryGetValue(value, out var mapped))
                {
                    style.Val = mapped;
                }
            }
        }
    }

    private static void RewriteStyleReferences(Style style, IReadOnlyDictionary<string, string> styleIdMap)
    {
        RewriteStyleReference(style.BasedOn, styleIdMap);
        RewriteStyleReference(style.NextParagraphStyle, styleIdMap);
        RewriteStyleReference(style.LinkedStyle, styleIdMap);
    }

    private static void RewriteStyleReference(OpenXmlLeafElement? styleReference, IReadOnlyDictionary<string, string> styleIdMap)
    {
        switch (styleReference)
        {
            case BasedOn basedOn when basedOn.Val?.Value is { } basedOnValue && styleIdMap.TryGetValue(basedOnValue, out var mappedBasedOn):
                basedOn.Val = mappedBasedOn;
                break;
            case NextParagraphStyle nextStyle when nextStyle.Val?.Value is { } nextValue && styleIdMap.TryGetValue(nextValue, out var mappedNext):
                nextStyle.Val = mappedNext;
                break;
            case LinkedStyle linkedStyle when linkedStyle.Val?.Value is { } linkedValue && styleIdMap.TryGetValue(linkedValue, out var mappedLinked):
                linkedStyle.Val = mappedLinked;
                break;
        }
    }

    private static bool AreEquivalent(
        Style sourceStyle,
        Style destinationStyle,
        Dictionary<Style, string> normalizedStyleXml)
    {
        return NormalizeStyleXml(sourceStyle, normalizedStyleXml) == NormalizeStyleXml(destinationStyle, normalizedStyleXml);
    }

    private static string NormalizeStyleXml(Style style, Dictionary<Style, string> normalizedStyleXml)
    {
        if (normalizedStyleXml.TryGetValue(style, out var existing))
        {
            return existing;
        }

        var clone = (Style)style.CloneNode(true);
        clone.StyleId = "__normalized-style__";
        var normalizedXml = clone.OuterXml;
        normalizedStyleXml[style] = normalizedXml;
        return normalizedXml;
    }

    private static string GenerateUniqueStyleId(string baseId, ISet<string> existingIds)
    {
        var suffix = 1;
        var candidate = $"{baseId}_imported";
        while (existingIds.Contains(candidate))
        {
            candidate = $"{baseId}_imported{suffix++}";
        }

        return candidate;
    }

    private static void MergeStylesWithEffects(
        MainDocumentPart sourceMainPart,
        IReadOnlyCollection<string> usedStyleIds,
        IReadOnlyDictionary<string, string> styleIdMap,
        MergeContext context,
        Dictionary<Style, string> normalizedStyleXml)
    {
        if (sourceMainPart.StylesWithEffectsPart?.Styles is null)
        {
            return;
        }

        var destinationPart = OpenXmlPartHelpers.EnsureStylesWithEffectsPart(context.MainPart);
        var destinationStyles = destinationPart.Styles!;
        var destinationById = destinationStyles.Elements<Style>()
            .Where(style => !string.IsNullOrWhiteSpace(style.StyleId))
            .ToDictionary(style => style.StyleId!.Value!, style => style, StringComparer.Ordinal);
        var sourceStyles = sourceMainPart.StylesWithEffectsPart.Styles.Elements<Style>()
            .Where(style => !string.IsNullOrWhiteSpace(style.StyleId))
            .ToDictionary(style => style.StyleId!.Value!, style => style, StringComparer.Ordinal);

        foreach (var usedStyleId in usedStyleIds)
        {
            if (!sourceStyles.TryGetValue(usedStyleId, out var sourceStyle) ||
                !styleIdMap.TryGetValue(usedStyleId, out var destinationStyleId))
            {
                continue;
            }

            if (destinationById.TryGetValue(destinationStyleId, out var existingStyle) &&
                AreEquivalent(sourceStyle, existingStyle, normalizedStyleXml))
            {
                continue;
            }

            var clone = (Style)sourceStyle.CloneNode(true);
            clone.StyleId = destinationStyleId;
            RewriteStyleReferences(clone, styleIdMap);
            destinationStyles.AppendChild(clone);
            destinationById[destinationStyleId] = clone;
        }
    }
}
