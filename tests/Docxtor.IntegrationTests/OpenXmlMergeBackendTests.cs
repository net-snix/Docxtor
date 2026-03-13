using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Docxtor.Core.Models;
using Docxtor.Core.Services;
using Docxtor.IntegrationTests.Support;
using Docxtor.OpenXml;

namespace Docxtor.IntegrationTests;

public sealed class OpenXmlMergeBackendTests
{
    [Fact]
    public async Task MergeAsync_preserves_paragraphs_table_image_and_hyperlink_relationships()
    {
        using var workspace = new TemporaryTestDirectory();
        var firstInput = DocxFixtureFactory.CreateBasicDocument(
            System.IO.Path.Combine(workspace.Path, "one.docx"),
            "Alpha paragraph",
            "Alpha cell",
            "https://example.com/alpha",
            "Alpha link");
        var secondInput = DocxFixtureFactory.CreateBasicDocument(
            System.IO.Path.Combine(workspace.Path, "two.docx"),
            "Beta paragraph",
            "Beta cell",
            "https://example.com/beta",
            "Beta link");
        var outputPath = System.IO.Path.Combine(workspace.Path, "merged.docx");

        var result = await CreateMerger().MergeAsync(CreateJob(outputPath, firstInput, secondInput));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Report.Errors.Select(error => error.Message)));
        using var merged = WordprocessingDocument.Open(outputPath, false);
        var bodyText = merged.MainDocumentPart!.Document!.Body!.InnerText;

        Assert.Contains("Alpha paragraph", bodyText);
        Assert.Contains("Beta paragraph", bodyText);
        Assert.True(merged.MainDocumentPart.Document.Body.Elements<Table>().Count() >= 2);
        Assert.NotEmpty(merged.MainDocumentPart.HyperlinkRelationships);
        Assert.Contains(merged.MainDocumentPart.Parts, part => part.OpenXmlPart is ImagePart);
    }

    [Fact]
    public async Task MergeAsync_preserves_table_look_markup_without_openxml_validation_errors()
    {
        using var workspace = new TemporaryTestDirectory();
        var firstInput = DocxFixtureFactory.CreateTableLookDocument(
            System.IO.Path.Combine(workspace.Path, "table-look-one.docx"),
            "Alpha paragraph",
            "Alpha cell");
        var secondInput = DocxFixtureFactory.CreateTableLookDocument(
            System.IO.Path.Combine(workspace.Path, "table-look-two.docx"),
            "Beta paragraph",
            "Beta cell");
        var outputPath = System.IO.Path.Combine(workspace.Path, "merged-table-look.docx");

        var result = await CreateMerger().MergeAsync(CreateJob(outputPath, firstInput, secondInput));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Report.Errors.Select(error => error.Message)));
        Assert.Equal(ValidationOutcome.Passed, result.Report.OpenXmlValidation.Outcome);

        using var merged = WordprocessingDocument.Open(outputPath, false);
        var tableLooks = merged.MainDocumentPart!.Document!.Body!
            .Descendants<TableLook>()
            .ToArray();

        Assert.NotEmpty(tableLooks);
        Assert.All(tableLooks, tableLook => Assert.True(tableLook.FirstRow?.Value ?? false));
    }

    [Fact]
    public async Task MergeAsync_adds_update_fields_setting_without_openxml_validation_errors()
    {
        using var workspace = new TemporaryTestDirectory();
        var firstInput = DocxFixtureFactory.CreateSettingsDocument(
            System.IO.Path.Combine(workspace.Path, "settings-one.docx"),
            "Alpha paragraph");
        var secondInput = DocxFixtureFactory.CreateSettingsDocument(
            System.IO.Path.Combine(workspace.Path, "settings-two.docx"),
            "Beta paragraph");
        var outputPath = System.IO.Path.Combine(workspace.Path, "merged-settings.docx");

        var result = await CreateMerger().MergeAsync(CreateJob(outputPath, firstInput, secondInput));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Report.Errors.Select(error => error.Message)));
        Assert.Equal(ValidationOutcome.Passed, result.Report.OpenXmlValidation.Outcome);

        using var merged = WordprocessingDocument.Open(outputPath, false);
        var updateFields = merged.MainDocumentPart!
            .DocumentSettingsPart!
            .Settings!
            .Elements<UpdateFieldsOnOpen>()
            .Single();

        Assert.True(updateFields.Val?.Value ?? false);
    }

    [Fact]
    public async Task MergeAsync_preserves_numbering_without_openxml_validation_errors()
    {
        using var workspace = new TemporaryTestDirectory();
        var firstInput = DocxFixtureFactory.CreateNumberedListDocument(
            System.IO.Path.Combine(workspace.Path, "numbering-one.docx"),
            "Alpha item",
            "Alpha follow-up");
        var secondInput = DocxFixtureFactory.CreateNumberedListDocument(
            System.IO.Path.Combine(workspace.Path, "numbering-two.docx"),
            "Beta item",
            "Beta follow-up");
        var outputPath = System.IO.Path.Combine(workspace.Path, "merged-numbering.docx");

        var result = await CreateMerger().MergeAsync(CreateJob(outputPath, firstInput, secondInput));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Report.Errors.Select(error => error.Message)));
        Assert.Equal(ValidationOutcome.Passed, result.Report.OpenXmlValidation.Outcome);

        using var merged = WordprocessingDocument.Open(outputPath, false);
        var numberingPart = merged.MainDocumentPart!.NumberingDefinitionsPart!;
        var numbering = numberingPart.Numbering!;

        Assert.True(numbering.Elements<AbstractNum>().Count() >= 1);
        Assert.True(numbering.Elements<NumberingInstance>().Count() >= 1);
        Assert.Contains(
            merged.MainDocumentPart.Document!.Body!.Descendants<NumberingId>(),
            numberingId => numberingId.Val?.Value is not null);
    }

    [Fact]
    public async Task MergeAsync_inserts_source_file_titles_before_each_segment_when_enabled()
    {
        using var workspace = new TemporaryTestDirectory();
        var firstInput = DocxFixtureFactory.CreateSettingsDocument(
            System.IO.Path.Combine(workspace.Path, "alpha-one.docx"),
            "Alpha paragraph");
        var secondInput = DocxFixtureFactory.CreateSettingsDocument(
            System.IO.Path.Combine(workspace.Path, "beta-two.docx"),
            "Beta paragraph");
        var outputPath = System.IO.Path.Combine(workspace.Path, "merged-titles.docx");

        var result = await CreateMerger().MergeAsync(CreateJob(outputPath, insertSourceFileTitles: true, firstInput, secondInput));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Report.Errors.Select(error => error.Message)));
        Assert.Equal(ValidationOutcome.Passed, result.Report.OpenXmlValidation.Outcome);

        using var merged = WordprocessingDocument.Open(outputPath, false);
        var paragraphs = merged.MainDocumentPart!.Document!.Body!
            .Elements<Paragraph>()
            .Select(paragraph => paragraph.InnerText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToArray();

        Assert.Equal(
            ["alpha-one", "Alpha paragraph", "beta-two", "Beta paragraph"],
            paragraphs);
    }

    [Fact]
    public async Task InspectAsync_fails_when_tracked_changes_are_present()
    {
        using var workspace = new TemporaryTestDirectory();
        var trackedDocument = DocxFixtureFactory.CreateTrackedChangesDocument(
            System.IO.Path.Combine(workspace.Path, "tracked.docx"),
            "Tracked content");

        var result = await new OpenXmlMergeBackend().InspectAsync(
            [InputDocument.FromPath(trackedDocument, 0)],
            new MergePolicy());

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Code == "tracked-changes-present");
    }

    [Fact]
    public async Task InspectAsync_fails_when_altchunk_is_present()
    {
        using var workspace = new TemporaryTestDirectory();
        var altChunkDocument = DocxFixtureFactory.CreateAltChunkDocument(
            System.IO.Path.Combine(workspace.Path, "altchunk.docx"));

        var result = await new OpenXmlMergeBackend().InspectAsync(
            [InputDocument.FromPath(altChunkDocument, 0)],
            new MergePolicy());

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Code == "altchunk-present");
    }

    [Fact]
    public async Task InspectAsync_reports_basic_document_feature_inventory()
    {
        using var workspace = new TemporaryTestDirectory();
        var basicDocument = DocxFixtureFactory.CreateBasicDocument(
            System.IO.Path.Combine(workspace.Path, "basic.docx"),
            "Alpha paragraph",
            "Alpha cell",
            "https://example.com/alpha",
            "Alpha link");

        var result = await new OpenXmlMergeBackend().InspectAsync(
            [InputDocument.FromPath(basicDocument, 0)],
            new MergePolicy());

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Errors.Select(error => error.Message)));

        var inventory = Assert.Single(result.Inputs);
        Assert.False(inventory.HasHeaders);
        Assert.False(inventory.HasFooters);
        Assert.False(inventory.HasTrackedChanges);
        Assert.False(inventory.HasExternalImages);
        Assert.True(inventory.HasExternalHyperlinks);
        Assert.False(inventory.HasBookmarks);
        Assert.False(inventory.HasNumbering);
        Assert.False(inventory.HasAltChunk);
        Assert.False(inventory.HasFields);
        Assert.False(inventory.HasContentControls);
        Assert.True(inventory.PartCounts.ContainsKey(nameof(MainDocumentPart)));
        Assert.True(inventory.PartCounts.ContainsKey(nameof(ImagePart)));
        Assert.True(inventory.RelationshipCounts.TryGetValue(HyperlinkRelationshipType, out var hyperlinkCount) && hyperlinkCount >= 1);
        Assert.True(inventory.RelationshipCounts.TryGetValue(ImageRelationshipType, out var imageCount) && imageCount >= 1);
    }

    [Fact]
    public async Task MergeAsync_preserves_referenced_footnotes()
    {
        using var workspace = new TemporaryTestDirectory();
        var firstInput = DocxFixtureFactory.CreateFootnoteDocument(
            System.IO.Path.Combine(workspace.Path, "footnote-one.docx"),
            "Body one",
            "Footnote one");
        var secondInput = DocxFixtureFactory.CreateFootnoteDocument(
            System.IO.Path.Combine(workspace.Path, "footnote-two.docx"),
            "Body two",
            "Footnote two");
        var outputPath = System.IO.Path.Combine(workspace.Path, "merged-footnotes.docx");

        var result = await CreateMerger().MergeAsync(CreateJob(outputPath, firstInput, secondInput));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Report.Errors.Select(error => error.Message)));
        using var merged = WordprocessingDocument.Open(outputPath, false);
        var footnoteTexts = merged.MainDocumentPart!.FootnotesPart!.Footnotes!
            .Elements<Footnote>()
            .Where(footnote => footnote.Id?.Value > 0)
            .Select(footnote => footnote.InnerText)
            .ToArray();
        var footnoteReferenceCount = merged.MainDocumentPart.Document!.Body!
            .Descendants<FootnoteReference>()
            .Count();

        Assert.Contains(footnoteTexts, text => text.Contains("Footnote one", StringComparison.Ordinal));
        Assert.Contains(footnoteTexts, text => text.Contains("Footnote two", StringComparison.Ordinal));
        Assert.Equal(2, footnoteReferenceCount);
    }

    [Fact]
    public async Task MergeAsync_preserves_headers_and_footers_for_each_section()
    {
        using var workspace = new TemporaryTestDirectory();
        var firstInput = DocxFixtureFactory.CreateHeaderFooterDocument(
            System.IO.Path.Combine(workspace.Path, "headers-one.docx"),
            "Body one",
            "Header one",
            "Footer one");
        var secondInput = DocxFixtureFactory.CreateHeaderFooterDocument(
            System.IO.Path.Combine(workspace.Path, "headers-two.docx"),
            "Body two",
            "Header two",
            "Footer two");
        var outputPath = System.IO.Path.Combine(workspace.Path, "merged-headers.docx");

        var result = await CreateMerger().MergeAsync(CreateJob(outputPath, firstInput, secondInput));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Report.Errors.Select(error => error.Message)));
        using var merged = WordprocessingDocument.Open(outputPath, false);
        var headers = merged.MainDocumentPart!.HeaderParts.Select(part => part.Header!.InnerText).ToArray();
        var footers = merged.MainDocumentPart.FooterParts.Select(part => part.Footer!.InnerText).ToArray();

        Assert.Contains(headers, text => text.Contains("Header one", StringComparison.Ordinal));
        Assert.Contains(headers, text => text.Contains("Header two", StringComparison.Ordinal));
        Assert.Contains(footers, text => text.Contains("Footer one", StringComparison.Ordinal));
        Assert.Contains(footers, text => text.Contains("Footer two", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MergeAsync_preserves_many_hyperlink_relationships_without_lookup_failures()
    {
        using var workspace = new TemporaryTestDirectory();
        var firstInput = CreateHyperlinkHeavyDocument(
            System.IO.Path.Combine(workspace.Path, "hyperlinks-one.docx"),
            "alpha",
            64);
        var secondInput = CreateHyperlinkHeavyDocument(
            System.IO.Path.Combine(workspace.Path, "hyperlinks-two.docx"),
            "beta",
            64);
        var outputPath = System.IO.Path.Combine(workspace.Path, "merged-hyperlinks.docx");

        var result = await CreateMerger().MergeAsync(CreateJob(outputPath, firstInput, secondInput));

        Assert.True(result.Success, string.Join(Environment.NewLine, result.Report.Errors.Select(error => error.Message)));
        using var merged = WordprocessingDocument.Open(outputPath, false);
        var bodyText = merged.MainDocumentPart!.Document!.Body!.InnerText;

        Assert.Equal(128, merged.MainDocumentPart.HyperlinkRelationships.Count());
        Assert.Contains("alpha link 0", bodyText);
        Assert.Contains("beta link 63", bodyText);
    }

    private static DocxtorMerger CreateMerger()
    {
        return new DocxtorMerger([new OpenXmlMergeBackend()]);
    }

    private const string HyperlinkRelationshipType =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink";

    private const string ImageRelationshipType =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";

    private static MergeJob CreateJob(string outputPath, params string[] inputs)
    {
        return CreateJob(outputPath, false, inputs);
    }

    private static MergeJob CreateJob(string outputPath, bool insertSourceFileTitles, params string[] inputs)
    {
        return new MergeJob
        {
            Inputs = inputs.Select((path, index) => InputDocument.FromPath(path, index)).ToArray(),
            OutputPath = outputPath,
            ReportPath = System.IO.Path.ChangeExtension(outputPath, ".json"),
            BackendHint = "openxml-sdk",
            Policy = new MergePolicy
            {
                InsertSourceFileTitles = insertSourceFileTitles,
            },
            Validation = new ValidationPolicy
            {
                RunVisualRegression = false,
            },
        };
    }

    private static string CreateHyperlinkHeavyDocument(string path, string prefix, int hyperlinkCount)
    {
        using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        var body = new Body();
        mainPart.Document = new Document(body);

        for (var index = 0; index < hyperlinkCount; index++)
        {
            var relationship = mainPart.AddHyperlinkRelationship(
                new Uri($"https://example.com/{prefix}/{index}"),
                true);
            body.Append(
                new Paragraph(
                    new Hyperlink(new Run(new Text($"{prefix} link {index}")))
                    {
                        Id = relationship.Id,
                    }));
        }

        body.Append(new SectionProperties());
        mainPart.Document.Save();
        return path;
    }
}
