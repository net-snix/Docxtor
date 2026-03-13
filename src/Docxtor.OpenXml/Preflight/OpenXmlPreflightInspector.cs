using System.IO.Compression;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Docxtor.Core.Models;
using Docxtor.OpenXml.Internal;

namespace Docxtor.OpenXml.Preflight;

internal sealed class OpenXmlPreflightInspector(string backendName, string backendVersion, BackendCapabilities capabilities)
{
    private static readonly HashSet<string> RevisionElementNames =
    [
        "ins",
        "del",
        "moveFrom",
        "moveTo",
        "moveFromRangeStart",
        "moveFromRangeEnd",
        "moveToRangeStart",
        "moveToRangeEnd",
        "pPrChange",
        "rPrChange",
        "tblPrChange",
        "trPrChange",
        "tcPrChange",
        "sectPrChange",
    ];

    public Task<PreflightResult> InspectAsync(
        IReadOnlyList<InputDocument> inputs,
        MergePolicy policy,
        CancellationToken cancellationToken = default)
    {
        var inventories = new List<FeatureInventory>(inputs.Count);
        var warnings = new List<DiagnosticMessage>();
        var errors = new List<DiagnosticMessage>();

        foreach (var input in inputs.OrderBy(item => item.SourceIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                inventories.Add(InspectDocument(input.PathOrId));
            }
            catch (Exception ex) when (ex is OpenXmlPackageException or InvalidDataException or IOException)
            {
                errors.Add(new DiagnosticMessage
                {
                    Code = "corrupted-input",
                    Message = $"Could not open '{input.PathOrId}' as a valid DOCX package: {ex.Message}",
                    InputPath = input.PathOrId,
                });
            }
        }

        EvaluatePolicySupport(policy, warnings, errors);

        foreach (var inventory in inventories)
        {
            EvaluateInventory(policy, inventory, warnings, errors);
        }

        return Task.FromResult(new PreflightResult
        {
            Success = errors.Count == 0,
            Backend = backendName,
            BackendVersion = backendVersion,
            Inputs = inventories,
            Warnings = warnings,
            Errors = errors,
        });
    }

    private FeatureInventory InspectDocument(string inputPath)
    {
        using var document = WordprocessingDocument.Open(inputPath, false);
        var mainPart = document.MainDocumentPart ?? throw new InvalidDataException("Missing main document part.");
        var scan = ScanDocument(mainPart);

        return new FeatureInventory
        {
            InputPath = inputPath,
            HasHeaders = mainPart.HeaderParts.Any(),
            HasFooters = mainPart.FooterParts.Any(),
            HasFootnotes = mainPart.FootnotesPart?.Footnotes?.Elements<Footnote>().Any(item => IsRegularNoteId(item.Id?.Value.ToString())) == true,
            HasEndnotes = mainPart.EndnotesPart?.Endnotes?.Elements<Endnote>().Any(item => IsRegularNoteId(item.Id?.Value.ToString())) == true,
            HasComments = mainPart.WordprocessingCommentsPart?.Comments?.Elements<Comment>().Any() == true,
            HasTrackedChanges = scan.HasTrackedChanges,
            HasCharts = scan.HasCharts,
            HasSmartArt = scan.HasSmartArt,
            HasEmbeddedObjects = scan.HasEmbeddedObjects,
            HasTextBoxes = scan.HasTextBoxes,
            HasExternalHyperlinks = scan.HasExternalHyperlinks,
            HasExternalImages = scan.HasExternalImages,
            HasBookmarks = scan.HasBookmarks,
            HasNumbering = mainPart.NumberingDefinitionsPart is not null && scan.HasNumbering,
            HasStyles = mainPart.StyleDefinitionsPart is not null,
            HasStylesWithEffects = mainPart.StylesWithEffectsPart is not null,
            HasTheme = mainPart.ThemePart is not null,
            HasAltChunk = scan.HasAltChunk,
            HasFields = scan.HasFields,
            HasContentControls = scan.HasContentControls,
            PartCounts = scan.PartCounts,
            RelationshipCounts = scan.RelationshipCounts,
        };
    }

    private static DocumentScanResult ScanDocument(MainDocumentPart mainPart)
    {
        var scan = new DocumentScanResult();

        foreach (var part in OpenXmlPartHelpers.EnumerateParts(mainPart))
        {
            var partTypeName = part.GetType().Name;
            IncrementCount(scan.PartCounts, partTypeName);

            scan.HasCharts |= partTypeName.Contains("Chart", StringComparison.Ordinal);
            scan.HasSmartArt |= partTypeName.Contains("Diagram", StringComparison.Ordinal);
            scan.HasEmbeddedObjects |= partTypeName.Contains("Embedded", StringComparison.Ordinal);
            scan.HasAltChunk |= partTypeName.Contains("AlternativeFormatImport", StringComparison.Ordinal);

            foreach (var childPart in part.Parts)
            {
                IncrementCount(scan.RelationshipCounts, childPart.OpenXmlPart.RelationshipType);
            }

            foreach (var externalRelationship in part.ExternalRelationships)
            {
                IncrementCount(scan.RelationshipCounts, externalRelationship.RelationshipType);
                scan.HasExternalHyperlinks |= externalRelationship.RelationshipType.Contains("hyperlink", StringComparison.OrdinalIgnoreCase);
                scan.HasExternalImages |= externalRelationship.RelationshipType.Contains("image", StringComparison.OrdinalIgnoreCase);
            }

            foreach (var hyperlinkRelationship in part.HyperlinkRelationships)
            {
                IncrementCount(scan.RelationshipCounts, hyperlinkRelationship.RelationshipType);
                scan.HasExternalHyperlinks = true;
            }

            if (part.RootElement is null)
            {
                continue;
            }

            foreach (var element in OpenXmlPartHelpers.SelfAndDescendants(part.RootElement))
            {
                scan.HasTrackedChanges |= RevisionElementNames.Contains(element.LocalName);
                scan.HasTextBoxes |= element is TextBoxContent || element.LocalName == "txbxContent";
                scan.HasBookmarks |= element is BookmarkStart;
                scan.HasAltChunk |= element is AltChunk;
                scan.HasFields |= element is FieldCode or SimpleField;
                scan.HasContentControls |= element is SdtElement;

                if (ReferenceEquals(part, mainPart))
                {
                    scan.HasNumbering |= element is NumberingId;
                }
            }
        }

        return scan;
    }

    private static void IncrementCount(Dictionary<string, int> counts, string key)
    {
        counts[key] = counts.TryGetValue(key, out var existingCount)
            ? existingCount + 1
            : 1;
    }

    private void EvaluatePolicySupport(
        MergePolicy policy,
        List<DiagnosticMessage> warnings,
        List<DiagnosticMessage> errors)
    {
        if (!capabilities.SupportedBoundaryModes.Contains(policy.BoundaryMode) &&
            policy.SectionPolicy == SectionPolicy.PreserveSourceSections)
        {
            errors.Add(new DiagnosticMessage
            {
                Code = "unsupported-boundary",
                Message = $"Boundary mode '{policy.BoundaryMode}' is not supported when preserving source sections.",
            });
        }

        if (policy.NumberingMode == NumberingMode.ContinueDestination &&
            !capabilities.SupportsContinueDestinationNumbering)
        {
            errors.Add(new DiagnosticMessage
            {
                Code = "unsupported-numbering-mode",
                Message = "The openxml-sdk backend does not support continue-destination numbering yet.",
            });
        }

        if (policy.TrackedChangesMode is not TrackedChangesMode.Fail &&
            !capabilities.SupportsTrackedChangesNormalization)
        {
            errors.Add(new DiagnosticMessage
            {
                Code = "unsupported-tracked-changes-mode",
                Message = "Tracked-changes normalization is not implemented by the openxml-sdk backend.",
            });
        }

        if (policy.AltChunkMode is not AltChunkMode.Reject &&
            !capabilities.SupportsAltChunkResolution)
        {
            errors.Add(new DiagnosticMessage
            {
                Code = "unsupported-altchunk-mode",
                Message = "altChunk resolution is not implemented by the openxml-sdk backend.",
            });
        }

        if (policy.ExternalResourceMode == ExternalResourceMode.Materialize)
        {
            errors.Add(new DiagnosticMessage
            {
                Code = "unsupported-external-resource-mode",
                Message = "External resource materialization is not supported. Preserve external links instead.",
            });
        }

        if (policy.SectionPolicy == SectionPolicy.UnifyWithBaseHeadersFooters)
        {
            warnings.Add(new DiagnosticMessage
            {
                Code = "section-flattening",
                Message = "Section flattening is enabled. Imported headers, footers, and page setup may be reduced to base-document behavior.",
            });
        }
    }

    private void EvaluateInventory(
        MergePolicy policy,
        FeatureInventory inventory,
        List<DiagnosticMessage> warnings,
        List<DiagnosticMessage> errors)
    {
        if (inventory.HasTrackedChanges && policy.TrackedChangesMode == TrackedChangesMode.Fail)
        {
            errors.Add(new DiagnosticMessage
            {
                Code = "tracked-changes-present",
                Message = "Tracked changes are present and the active policy is fail.",
                InputPath = inventory.InputPath,
            });
        }

        if (inventory.HasAltChunk)
        {
            errors.Add(new DiagnosticMessage
            {
                Code = "altchunk-present",
                Message = "altChunk content is present and must be resolved before merge.",
                InputPath = inventory.InputPath,
            });
        }

        if (inventory.HasCharts && !capabilities.SupportsCharts)
        {
            errors.Add(new DiagnosticMessage
            {
                Code = "unsupported-charts",
                Message = "Chart-bearing documents are not yet supported by the openxml-sdk backend.",
                InputPath = inventory.InputPath,
            });
        }

        if (inventory.HasSmartArt && !capabilities.SupportsSmartArt)
        {
            errors.Add(new DiagnosticMessage
            {
                Code = "unsupported-smartart",
                Message = "SmartArt or diagram-bearing documents are not yet supported by the openxml-sdk backend.",
                InputPath = inventory.InputPath,
            });
        }

        if (inventory.HasEmbeddedObjects && !capabilities.SupportsEmbeddedObjects)
        {
            errors.Add(new DiagnosticMessage
            {
                Code = "unsupported-embedded-objects",
                Message = "Embedded package or OLE content is not yet supported by the openxml-sdk backend.",
                InputPath = inventory.InputPath,
            });
        }

        if ((inventory.HasHeaders || inventory.HasFooters) &&
            policy.SectionPolicy == SectionPolicy.UnifyWithBaseHeadersFooters)
        {
            warnings.Add(new DiagnosticMessage
            {
                Code = "header-footer-unify",
                Message = "Imported headers and footers will be replaced by base-document behavior.",
                InputPath = inventory.InputPath,
            });
        }
    }

    private static bool IsRegularNoteId(string? idValue)
    {
        return int.TryParse(idValue, out var parsed) && parsed > 0;
    }

    private sealed class DocumentScanResult
    {
        public Dictionary<string, int> PartCounts { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, int> RelationshipCounts { get; } = new(StringComparer.Ordinal);

        public bool HasTrackedChanges { get; set; }

        public bool HasCharts { get; set; }

        public bool HasSmartArt { get; set; }

        public bool HasEmbeddedObjects { get; set; }

        public bool HasTextBoxes { get; set; }

        public bool HasExternalHyperlinks { get; set; }

        public bool HasExternalImages { get; set; }

        public bool HasBookmarks { get; set; }

        public bool HasNumbering { get; set; }

        public bool HasAltChunk { get; set; }

        public bool HasFields { get; set; }

        public bool HasContentControls { get; set; }
    }
}
