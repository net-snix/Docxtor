using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Pictures;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace Docxtor.IntegrationTests.Support;

internal static class DocxFixtureFactory
{
    private static readonly byte[] TinyPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
        0x00, 0x03, 0x01, 0x01, 0x00, 0x18, 0xDD, 0x8D,
        0xB1, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
        0x44, 0xAE, 0x42, 0x60, 0x82,
    ];

    public static string CreateBasicDocument(
        string path,
        string paragraphText,
        string tableCellText,
        string hyperlinkUrl,
        string hyperlinkText)
    {
        using var document = CreateEmptyDocument(path, out var mainPart, out var body);

        body.Append(new Paragraph(new Run(new Text(paragraphText))));
        body.Append(CreateTable(tableCellText));
        body.Append(CreateHyperlinkParagraph(mainPart, hyperlinkUrl, hyperlinkText));
        body.Append(new Paragraph(new Run(CreateImage(mainPart, "tiny.png", 1U))));
        body.Append(CreateSectionProperties());

        mainPart.Document!.Save();
        return path;
    }

    public static string CreateTableLookDocument(string path, string paragraphText, string tableCellText)
    {
        using var document = CreateEmptyDocument(path, out var mainPart, out var body);

        body.Append(new Paragraph(new Run(new Text(paragraphText))));
        body.Append(CreateTable(tableCellText, includeTableLook: true));
        body.Append(CreateSectionProperties());

        mainPart.Document!.Save();
        return path;
    }

    public static string CreateSettingsDocument(string path, string bodyText)
    {
        using var document = CreateEmptyDocument(path, out var mainPart, out var body);
        var settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
        settingsPart.Settings = new Settings(new Compatibility());

        body.Append(new Paragraph(new Run(new Text(bodyText))));
        body.Append(CreateSectionProperties());

        mainPart.Document!.Save();
        settingsPart.Settings.Save();
        return path;
    }

    public static string CreateNumberedListDocument(string path, string firstItemText, string secondItemText)
    {
        using var document = CreateEmptyDocument(path, out var mainPart, out var body);
        var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
        var level = new Level
        {
            LevelIndex = 0,
        };
        level.AddChild(new StartNumberingValue { Val = 1 }, true);
        level.AddChild(new NumberingFormat { Val = NumberFormatValues.Decimal }, true);
        level.AddChild(new LevelText { Val = "%1." }, true);

        var abstractNum = new AbstractNum
        {
            AbstractNumberId = 1,
        };
        abstractNum.AddChild(level, true);

        var numberingInstance = new NumberingInstance
        {
            NumberID = 1,
        };
        numberingInstance.AddChild(new AbstractNumId { Val = 1 }, true);

        var numbering = new Numbering();
        numbering.AppendChild(abstractNum);
        numbering.AppendChild(numberingInstance);
        numberingPart.Numbering = numbering;

        body.Append(CreateNumberedParagraph(firstItemText, 1));
        body.Append(CreateNumberedParagraph(secondItemText, 1));
        body.Append(CreateSectionProperties());

        mainPart.Document!.Save();
        numberingPart.Numbering.Save();
        return path;
    }

    public static string CreateStyledDocument(
        string path,
        string paragraphText,
        string styleId,
        string styleName)
    {
        using var document = CreateEmptyDocument(path, out var mainPart, out var body);
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = styleId,
                CustomStyle = OnOffValue.FromBoolean(true),
            });

        var style = stylesPart.Styles.GetFirstChild<Style>()!;
        style.Append(
            new StyleName { Val = styleName },
            new PrimaryStyle());

        body.Append(
            new Paragraph(
                new ParagraphProperties(
                    new ParagraphStyleId { Val = styleId }),
                new Run(new Text(paragraphText))));
        body.Append(CreateSectionProperties());

        mainPart.Document!.Save();
        stylesPart.Styles.Save();
        return path;
    }

    public static string CreateHeaderFooterDocument(string path, string bodyText, string headerText, string footerText)
    {
        using var document = CreateEmptyDocument(path, out var mainPart, out var body);

        var headerPart = mainPart.AddNewPart<HeaderPart>();
        headerPart.Header = new Header(new Paragraph(new Run(new Text(headerText))));

        var footerPart = mainPart.AddNewPart<FooterPart>();
        footerPart.Footer = new Footer(new Paragraph(new Run(new Text(footerText))));

        body.Append(new Paragraph(new Run(new Text(bodyText))));
        body.Append(CreateSectionProperties(
            headerRelationshipId: mainPart.GetIdOfPart(headerPart),
            footerRelationshipId: mainPart.GetIdOfPart(footerPart)));

        mainPart.Document!.Save();
        return path;
    }

    public static string CreateFootnoteDocument(string path, string bodyText, string footnoteText)
    {
        using var document = CreateEmptyDocument(path, out var mainPart, out var body);
        var footnotesPart = mainPart.AddNewPart<FootnotesPart>();
        footnotesPart.Footnotes = new Footnotes(
            new Footnote
            {
                Type = FootnoteEndnoteValues.Separator,
                Id = -1,
            },
            new Footnote
            {
                Type = FootnoteEndnoteValues.ContinuationSeparator,
                Id = 0,
            },
            new Footnote(
                new Paragraph(new Run(new Text(footnoteText))))
            {
                Id = 1,
            });

        body.Append(
            new Paragraph(
                new Run(new Text(bodyText)),
                new Run(new FootnoteReference { Id = 1 })));
        body.Append(CreateSectionProperties());

        mainPart.Document!.Save();
        return path;
    }

    public static string CreateTrackedChangesDocument(string path, string insertedText)
    {
        using var document = CreateEmptyDocument(path, out var mainPart, out var body);

        body.Append(new Paragraph(
            new InsertedRun(
                new Run(new Text(insertedText)))
            {
                Id = "1",
                Author = "Docxtor Tests",
                Date = DateTime.UtcNow,
            }));
        body.Append(CreateSectionProperties());

        mainPart.Document!.Save();
        return path;
    }

    public static string CreateAltChunkDocument(string path)
    {
        using var document = CreateEmptyDocument(path, out var mainPart, out var body);

        var altChunkPart = mainPart.AddAlternativeFormatImportPart(AlternativeFormatImportPartType.Html, "altChunkId1");
        using (var writer = new StreamWriter(altChunkPart.GetStream(FileMode.Create, FileAccess.Write)))
        {
            writer.Write("<html><body><p>Alt chunk body</p></body></html>");
        }

        body.Append(new AltChunk { Id = "altChunkId1" });
        body.Append(CreateSectionProperties());

        mainPart.Document!.Save();
        return path;
    }

    private static WordprocessingDocument CreateEmptyDocument(string path, out MainDocumentPart mainPart, out Body body)
    {
        var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        mainPart = document.AddMainDocumentPart();
        body = new Body();
        mainPart.Document = new Document(body);
        return document;
    }

    private static Table CreateTable(string cellText, bool includeTableLook = false)
    {
        var tableProperties = new TableProperties(
            new TableWidth { Type = TableWidthUnitValues.Auto, Width = "0" });

        if (includeTableLook)
        {
            tableProperties.AppendChild(new TableLook
            {
                FirstRow = OnOffValue.FromBoolean(true),
                LastRow = OnOffValue.FromBoolean(false),
                FirstColumn = OnOffValue.FromBoolean(false),
                LastColumn = OnOffValue.FromBoolean(false),
                NoHorizontalBand = OnOffValue.FromBoolean(false),
                NoVerticalBand = OnOffValue.FromBoolean(true),
                Val = "04A0",
            });
        }

        return new Table(
            tableProperties,
            new TableGrid(
                new GridColumn { Width = "2400" },
                new GridColumn { Width = "2400" }),
            new TableRow(
                new TableCell(new Paragraph(new Run(new Text(cellText)))),
                new TableCell(new Paragraph(new Run(new Text("B1"))))));
    }

    private static Paragraph CreateNumberedParagraph(string text, int numberingId)
    {
        return new Paragraph(
            new ParagraphProperties(
                new NumberingProperties(
                    new NumberingLevelReference { Val = 0 },
                    new NumberingId { Val = numberingId })),
            new Run(new Text(text)));
    }

    private static Paragraph CreateHyperlinkParagraph(MainDocumentPart mainPart, string hyperlinkUrl, string hyperlinkText)
    {
        var relationship = mainPart.AddHyperlinkRelationship(new Uri(hyperlinkUrl), true);
        return new Paragraph(
            new Hyperlink(
                new Run(
                    new RunProperties(
                        new Color { Val = "0563C1" },
                        new Underline { Val = UnderlineValues.Single }),
                    new Text(hyperlinkText)))
            {
                Id = relationship.Id,
                History = OnOffValue.FromBoolean(true),
            });
    }

    private static Drawing CreateImage(MainDocumentPart mainPart, string imageName, uint imageId)
    {
        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (var stream = imagePart.GetStream(FileMode.Create, FileAccess.Write))
        {
            stream.Write(TinyPng);
        }

        var relationshipId = mainPart.GetIdOfPart(imagePart);

        return new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = 990000L, Cy = 990000L },
                new DW.EffectExtent
                {
                    LeftEdge = 0L,
                    TopEdge = 0L,
                    RightEdge = 0L,
                    BottomEdge = 0L,
                },
                new DW.DocProperties { Id = imageId, Name = imageName },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = imageId, Name = imageName },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = relationshipId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = 990000L, Cy = 990000L }),
                                new A.PresetGeometry(new A.AdjustValueList())
                                {
                                    Preset = A.ShapeTypeValues.Rectangle,
                                })))
                    {
                        Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture",
                    }))
            {
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U,
                DistanceFromTop = 0U,
            });
    }

    private static SectionProperties CreateSectionProperties(string? headerRelationshipId = null, string? footerRelationshipId = null)
    {
        var children = new List<OpenXmlElement>();

        if (!string.IsNullOrWhiteSpace(headerRelationshipId))
        {
            children.Add(new HeaderReference
            {
                Type = HeaderFooterValues.Default,
                Id = headerRelationshipId,
            });
        }

        if (!string.IsNullOrWhiteSpace(footerRelationshipId))
        {
            children.Add(new FooterReference
            {
                Type = HeaderFooterValues.Default,
                Id = footerRelationshipId,
            });
        }

        children.Add(new PageSize { Width = 12240U, Height = 15840U });
        children.Add(new PageMargin
        {
            Top = 1440,
            Bottom = 1440,
            Left = 1440U,
            Right = 1440U,
            Header = 720U,
            Footer = 720U,
            Gutter = 0U,
        });

        return new SectionProperties(children);
    }
}
