using System.Security.Cryptography;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Docxtor.OpenXml.Internal;

internal static class OpenXmlPartHelpers
{
    public static StyleDefinitionsPart EnsureStylesPart(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.StyleDefinitionsPart ?? mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles ??= new Styles();
        return stylesPart;
    }

    public static StylesWithEffectsPart EnsureStylesWithEffectsPart(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.StylesWithEffectsPart ?? mainPart.AddNewPart<StylesWithEffectsPart>();
        stylesPart.Styles ??= new Styles();
        return stylesPart;
    }

    public static NumberingDefinitionsPart EnsureNumberingPart(MainDocumentPart mainPart)
    {
        var numberingPart = mainPart.NumberingDefinitionsPart ?? mainPart.AddNewPart<NumberingDefinitionsPart>();
        numberingPart.Numbering ??= new Numbering();
        return numberingPart;
    }

    public static FootnotesPart EnsureFootnotesPart(MainDocumentPart mainPart)
    {
        var footnotesPart = mainPart.FootnotesPart ?? mainPart.AddNewPart<FootnotesPart>();
        footnotesPart.Footnotes ??= new Footnotes(
            new Footnote
            {
                Type = FootnoteEndnoteValues.Separator,
                Id = -1,
            },
            new Footnote
            {
                Type = FootnoteEndnoteValues.ContinuationSeparator,
                Id = 0,
            });

        return footnotesPart;
    }

    public static EndnotesPart EnsureEndnotesPart(MainDocumentPart mainPart)
    {
        var endnotesPart = mainPart.EndnotesPart ?? mainPart.AddNewPart<EndnotesPart>();
        endnotesPart.Endnotes ??= new Endnotes(
            new Endnote
            {
                Type = FootnoteEndnoteValues.Separator,
                Id = -1,
            },
            new Endnote
            {
                Type = FootnoteEndnoteValues.ContinuationSeparator,
                Id = 0,
            });

        return endnotesPart;
    }

    public static WordprocessingCommentsPart EnsureCommentsPart(MainDocumentPart mainPart)
    {
        var commentsPart = mainPart.WordprocessingCommentsPart ?? mainPart.AddNewPart<WordprocessingCommentsPart>();
        commentsPart.Comments ??= new Comments();
        return commentsPart;
    }

    public static DocumentSettingsPart EnsureSettingsPart(MainDocumentPart mainPart)
    {
        var settingsPart = mainPart.DocumentSettingsPart ?? mainPart.AddNewPart<DocumentSettingsPart>();
        settingsPart.Settings ??= new Settings();
        return settingsPart;
    }

    public static IEnumerable<OpenXmlElement> SelfAndDescendants(OpenXmlElement root)
    {
        yield return root;

        foreach (var child in root.Descendants())
        {
            yield return child;
        }
    }

    public static IEnumerable<OpenXmlPart> EnumerateParts(MainDocumentPart mainPart)
    {
        var queue = new Queue<OpenXmlPart>();
        var visited = new HashSet<Uri>();

        queue.Enqueue(mainPart);
        visited.Add(mainPart.Uri);

        while (queue.Count > 0)
        {
            var part = queue.Dequeue();
            yield return part;

            foreach (var child in part.Parts.Select(item => item.OpenXmlPart))
            {
                if (visited.Add(child.Uri))
                {
                    queue.Enqueue(child);
                }
            }
        }
    }

    public static IEnumerable<OpenXmlElement> EnumerateRootElements(MainDocumentPart mainPart)
    {
        foreach (var part in EnumerateParts(mainPart))
        {
            if (part.RootElement is not null)
            {
                yield return part.RootElement;
            }
        }
    }

    public static uint NextUIntId(IEnumerable<string?> values)
    {
        var max = values
            .Select(value => uint.TryParse(value, out var parsed) ? parsed : 0U)
            .DefaultIfEmpty()
            .Max();

        return max + 1;
    }

    public static int NextIntId(IEnumerable<string?> values)
    {
        var max = values
            .Select(value => int.TryParse(value, out var parsed) ? parsed : 0)
            .Where(value => value > 0)
            .DefaultIfEmpty()
            .Max();

        return max + 1;
    }

    public static string ComputeHash(OpenXmlPart part)
    {
        using var stream = part.GetStream(FileMode.Open, FileAccess.Read);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }
}
