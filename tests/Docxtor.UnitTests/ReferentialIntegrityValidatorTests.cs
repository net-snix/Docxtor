using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
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
