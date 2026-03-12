namespace Docxtor.Core.Models;

public sealed record RemapSummary
{
    public int RelationshipIds { get; set; }

    public int Styles { get; set; }

    public int Numbering { get; set; }

    public int AbstractNumbering { get; set; }

    public int Footnotes { get; set; }

    public int Endnotes { get; set; }

    public int Comments { get; set; }

    public int BookmarkIds { get; set; }

    public int BookmarkNames { get; set; }

    public int DrawingIds { get; set; }

    public int PictureIds { get; set; }

    public int HeaderFooterParts { get; set; }

    public int ImagesDeduplicated { get; set; }
}
