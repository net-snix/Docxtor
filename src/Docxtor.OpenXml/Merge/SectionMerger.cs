using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Docxtor.Core.Models;
using Docxtor.OpenXml.Internal;

namespace Docxtor.OpenXml.Merge;

internal sealed class SectionMerger(RelationshipCopier relationshipCopier)
{
    public void AppendContent(
        MainDocumentPart sourceMainPart,
        IReadOnlyList<OpenXmlElement> importedElements,
        SectionProperties? sourceFinalSectionProperties,
        MergeContext context)
    {
        var destinationDocument = context.MainPart.Document ??= new Document(new Body());
        var destinationBody = destinationDocument.Body ??= new Body();

        if (context.Policy.SectionPolicy == SectionPolicy.PreserveSourceSections)
        {
            foreach (var sectionProperties in importedElements.SelectMany(root => root.Descendants<SectionProperties>()).ToArray())
            {
                RewriteSectionProperties(sectionProperties, sourceMainPart, context);
            }

            var importedFinalSectionProperties = sourceFinalSectionProperties is null
                ? new SectionProperties()
                : (SectionProperties)sourceFinalSectionProperties.CloneNode(true);
            RewriteSectionProperties(importedFinalSectionProperties, sourceMainPart, context);

            var currentFinalSectionProperties = ExtractBodySectionProperties(destinationBody) ?? new SectionProperties();
            currentFinalSectionProperties.Remove();

            if (destinationBody.ChildElements.Count > 0)
            {
                destinationBody.AppendChild(CreateBoundaryParagraph(currentFinalSectionProperties, context.Policy.BoundaryMode));
            }

            foreach (var importedElement in importedElements)
            {
                destinationBody.AppendChild(importedElement);
            }

            destinationBody.AppendChild(importedFinalSectionProperties);
            return;
        }

        context.AddWarning(
            "section-flattened",
            "Section preservation is disabled. Imported section boundaries are being flattened.");

        var retainedFinalSectionProperties = ExtractBodySectionProperties(destinationBody) ?? new SectionProperties();
        retainedFinalSectionProperties.Remove();

        if (ShouldInsertPageBreak(context.Policy.BoundaryMode) && destinationBody.ChildElements.Count > 0)
        {
            destinationBody.AppendChild(CreatePageBreakParagraph());
        }

        foreach (var importedElement in importedElements)
        {
            StripSectionProperties(importedElement);
            destinationBody.AppendChild(importedElement);
        }

        destinationBody.AppendChild(retainedFinalSectionProperties);
    }

    private void RewriteSectionProperties(
        SectionProperties sectionProperties,
        MainDocumentPart sourceMainPart,
        MergeContext context)
    {
        if (!context.Policy.PreserveHeadersFooters)
        {
            sectionProperties.RemoveAllChildren<HeaderReference>();
            sectionProperties.RemoveAllChildren<FooterReference>();
            return;
        }

        foreach (var headerReference in sectionProperties.Elements<HeaderReference>().ToArray())
        {
            if (headerReference.Id?.Value is { } relationshipId)
            {
                headerReference.Id = relationshipCopier.CopyRelationshipId(
                    sourceMainPart,
                    context.MainPart,
                    relationshipId,
                    context);
                context.RemapSummary.HeaderFooterParts++;
            }
        }

        foreach (var footerReference in sectionProperties.Elements<FooterReference>().ToArray())
        {
            if (footerReference.Id?.Value is { } relationshipId)
            {
                footerReference.Id = relationshipCopier.CopyRelationshipId(
                    sourceMainPart,
                    context.MainPart,
                    relationshipId,
                    context);
                context.RemapSummary.HeaderFooterParts++;
            }
        }

        if (sectionProperties.Elements<HeaderReference>().Any(reference => reference.Type?.Value == HeaderFooterValues.Even) ||
            sectionProperties.Elements<FooterReference>().Any(reference => reference.Type?.Value == HeaderFooterValues.Even))
        {
            var settings = OpenXmlPartHelpers.EnsureSettingsPart(context.MainPart).Settings!;
            if (settings.Elements<EvenAndOddHeaders>().FirstOrDefault() is null)
            {
                settings.AddChild(new EvenAndOddHeaders { Val = true }, true);
                settings.Save();
            }
        }
    }

    private static Paragraph CreateBoundaryParagraph(SectionProperties currentSectionProperties, BoundaryMode boundaryMode)
    {
        var clonedSectionProperties = (SectionProperties)currentSectionProperties.CloneNode(true);
        clonedSectionProperties.RemoveAllChildren<SectionType>();
        var sectionType = new SectionType { Val = ToSectionMark(boundaryMode) };
        var anchor = clonedSectionProperties
            .Elements<OpenXmlElement>()
            .LastOrDefault(element => element is HeaderReference or FooterReference);

        if (anchor is null)
        {
            clonedSectionProperties.PrependChild(sectionType);
        }
        else
        {
            clonedSectionProperties.InsertAfter(sectionType, anchor);
        }

        return new Paragraph(
            new ParagraphProperties(clonedSectionProperties));
    }

    private static SectionMarkValues ToSectionMark(BoundaryMode boundaryMode)
    {
        return boundaryMode switch
        {
            BoundaryMode.ContinuousSection => SectionMarkValues.Continuous,
            _ => SectionMarkValues.NextPage,
        };
    }

    private static bool ShouldInsertPageBreak(BoundaryMode boundaryMode)
    {
        return boundaryMode is BoundaryMode.PageBreak or BoundaryMode.SectionNewPage;
    }

    private static Paragraph CreatePageBreakParagraph()
    {
        return new Paragraph(
            new Run(
                new Break { Type = BreakValues.Page }));
    }

    private static void StripSectionProperties(OpenXmlElement root)
    {
        foreach (var paragraphProperties in root.Descendants<ParagraphProperties>().ToArray())
        {
            paragraphProperties.RemoveAllChildren<SectionProperties>();
        }
    }

    private static SectionProperties? ExtractBodySectionProperties(Body body)
    {
        return body.Elements<SectionProperties>().LastOrDefault();
    }
}
