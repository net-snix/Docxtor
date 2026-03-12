using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Docxtor.OpenXml.Internal;

namespace Docxtor.OpenXml.Merge;

internal sealed class NumberingMerger
{
    public void MergeNumberingForElements(
        MainDocumentPart sourceMainPart,
        IReadOnlyList<OpenXmlElement> contentRoots,
        MergeContext context)
    {
        if (contentRoots.Count == 0 || sourceMainPart.NumberingDefinitionsPart?.Numbering is null)
        {
            return;
        }

        var usedNumIds = contentRoots
            .SelectMany(root => root.Descendants<NumberingId>())
            .Select(item => item.Val?.Value)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .ToArray();

        if (usedNumIds.Length == 0)
        {
            return;
        }

        var sourceNumbering = sourceMainPart.NumberingDefinitionsPart.Numbering;
        var destinationPart = OpenXmlPartHelpers.EnsureNumberingPart(context.MainPart);
        var destinationNumbering = destinationPart.Numbering!;

        var sourceNumbers = sourceNumbering.Elements<NumberingInstance>()
            .Where(item => item.NumberID?.Value is not null)
            .ToDictionary(item => item.NumberID!.Value);
        var sourceAbstractNumbers = sourceNumbering.Elements<AbstractNum>()
            .Where(item => item.AbstractNumberId?.Value is not null)
            .ToDictionary(item => item.AbstractNumberId!.Value);
        var numIdMap = new Dictionary<int, int>();
        var abstractNumIdMap = new Dictionary<int, int>();

        foreach (var sourceNumId in usedNumIds)
        {
            if (!sourceNumbers.TryGetValue(sourceNumId, out var sourceNumberingInstance) ||
                sourceNumberingInstance.AbstractNumId?.Val is null)
            {
                continue;
            }

            var sourceAbstractId = sourceNumberingInstance.AbstractNumId.Val.Value;
            if (!sourceAbstractNumbers.TryGetValue(sourceAbstractId, out var sourceAbstractNum))
            {
                continue;
            }

            if (!abstractNumIdMap.TryGetValue(sourceAbstractId, out var destinationAbstractId))
            {
                var existingAbstract = destinationNumbering.Elements<AbstractNum>().FirstOrDefault(item =>
                    NormalizeAbstractNum(item) == NormalizeAbstractNum(sourceAbstractNum));

                if (existingAbstract?.AbstractNumberId?.Value is int existingAbstractId)
                {
                    destinationAbstractId = existingAbstractId;
                }
                else
                {
                    destinationAbstractId = context.NextAbstractNumberingId();
                    var clonedAbstract = (AbstractNum)sourceAbstractNum.CloneNode(true);
                    clonedAbstract.AbstractNumberId = destinationAbstractId;
                    InsertAbstractNumber(destinationNumbering, clonedAbstract);
                    context.RemapSummary.AbstractNumbering++;
                }

                abstractNumIdMap[sourceAbstractId] = destinationAbstractId;
            }

            var destinationNumId = context.NextNumberingId();
            var clonedNumberingInstance = (NumberingInstance)sourceNumberingInstance.CloneNode(true);
            clonedNumberingInstance.NumberID = destinationNumId;
            clonedNumberingInstance.AbstractNumId ??= new AbstractNumId();
            clonedNumberingInstance.AbstractNumId.Val = destinationAbstractId;
            InsertNumberingInstance(destinationNumbering, clonedNumberingInstance);
            numIdMap[sourceNumId] = destinationNumId;
            context.RemapSummary.Numbering++;
        }

        foreach (var numberingId in contentRoots.SelectMany(root => root.Descendants<NumberingId>()))
        {
            if (numberingId.Val?.Value is int value && numIdMap.TryGetValue(value, out var mapped))
            {
                numberingId.Val = mapped;
            }
        }
    }

    private static string NormalizeAbstractNum(AbstractNum abstractNum)
    {
        var clone = (AbstractNum)abstractNum.CloneNode(true);
        clone.AbstractNumberId = 0;
        return clone.OuterXml;
    }

    private static void InsertAbstractNumber(Numbering numbering, AbstractNum abstractNum)
    {
        var anchor = numbering.ChildElements
            .FirstOrDefault(element => element is NumberingInstance or NumberingIdMacAtCleanup);

        if (anchor is null)
        {
            numbering.AppendChild(abstractNum);
            return;
        }

        numbering.InsertBefore(abstractNum, anchor);
    }

    private static void InsertNumberingInstance(Numbering numbering, NumberingInstance numberingInstance)
    {
        var cleanup = numbering.Elements<NumberingIdMacAtCleanup>().FirstOrDefault();
        if (cleanup is null)
        {
            numbering.AppendChild(numberingInstance);
            return;
        }

        numbering.InsertBefore(numberingInstance, cleanup);
    }
}
