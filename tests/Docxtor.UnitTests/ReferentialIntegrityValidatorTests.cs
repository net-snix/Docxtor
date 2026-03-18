using DocumentFormat.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Docxtor.Core.Models;
using Docxtor.OpenXml.Internal;
using Docxtor.Validation;

namespace Docxtor.UnitTests;

public sealed class ReferentialIntegrityValidatorTests
{
    [Fact]
    public async Task ValidateAsync_reports_dangling_hyperlink_relationships()
    {
        using var sandbox = new TemporaryDirectory();
        var documentPath = Path.Combine(sandbox.Path, "dangling-relationship.docx");

        using (var document = WordprocessingDocument.Create(documentPath, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(
                new Body(
                    new Paragraph(
                        new Hyperlink(new Run(new Text("Broken link")))
                        {
                            Id = "rIdMissing",
                        }),
                    new SectionProperties()));
            mainPart.Document.Save();
        }

        var summary = await new ReferentialIntegrityValidator().ValidateAsync(documentPath);

        Assert.Equal(Core.Models.ValidationOutcome.Failed, summary.Outcome);
        Assert.Contains(summary.Messages, message => message.Code == "dangling-relationship");
    }

    [Fact]
    public async Task ValidateAsync_reports_duplicate_bookmark_ids_once()
    {
        using var sandbox = new TemporaryDirectory();
        var documentPath = Path.Combine(sandbox.Path, "duplicate-bookmark-id.docx");

        using (var document = WordprocessingDocument.Create(documentPath, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(
                new Body(
                    new Paragraph(new BookmarkStart { Id = "42", Name = "first" }, new BookmarkEnd { Id = "42" }),
                    new Paragraph(new BookmarkStart { Id = "42", Name = "second" }, new BookmarkEnd { Id = "42" }),
                    new SectionProperties()));
            mainPart.Document.Save();
        }

        var summary = await new ReferentialIntegrityValidator().ValidateAsync(documentPath);
        var duplicateMessages = summary.Messages
            .Where(message => message.Code == "duplicate-bookmark-id")
            .ToArray();

        Assert.Equal(Core.Models.ValidationOutcome.Failed, summary.Outcome);
        Assert.Single(duplicateMessages);
        Assert.Contains("42", duplicateMessages[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MergeContext_Create_scans_root_elements_once_for_bookmarks_and_drawing_ids()
    {
        using var sandbox = new TemporaryDirectory();
        var documentPath = Path.Combine(sandbox.Path, "merge-context.docx");
        CreateDocumentWithImage(documentPath);

        using (var document = WordprocessingDocument.Open(documentPath, true))
        {
            var mainPart = document.MainDocumentPart!;
            var bookmarkParagraph = new Paragraph(
                new BookmarkStart { Id = "42", Name = "chapter-start" },
                new Run(new Text("Bookmark")),
                new BookmarkEnd { Id = "42" });
            var sectionProperties = mainPart.Document!.Body!.Elements<SectionProperties>().Single();

            mainPart.Document.Body.InsertBefore(bookmarkParagraph, sectionProperties);
            mainPart.Document.Save();
        }

        using var reopened = WordprocessingDocument.Open(documentPath, false);
        var context = MergeContext.Create(reopened.MainDocumentPart!, new MergePolicy());

        Assert.Equal(43U, context.NextBookmarkId());
        Assert.Equal(2U, context.NextDocPropertiesId());
        Assert.Equal(2U, context.NextPictureId());
        Assert.Contains("chapter-start", context.BookmarkNames);
    }

    private static void CreateDocumentWithImage(string path)
    {
        using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        var body = new Body();
        mainPart.Document = new Document(body);

        body.Append(new Paragraph(new Run(new Text("Alpha paragraph"))));
        body.Append(new Paragraph(new Run(CreateImage(mainPart, "tiny.png", 1U))));
        body.Append(CreateSectionProperties());

        mainPart.Document.Save();
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

    private static SectionProperties CreateSectionProperties()
    {
        return new SectionProperties(
            new PageSize { Width = 12240U, Height = 15840U },
            new PageMargin
            {
                Top = 1440,
                Right = 1440U,
                Bottom = 1440,
                Left = 1440U,
                Header = 720U,
                Footer = 720U,
                Gutter = 0U,
            },
            new Columns { Space = "720" },
            new DocGrid { LinePitch = 360 });
    }

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

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"docxtor-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
