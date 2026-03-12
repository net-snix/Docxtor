namespace Docxtor.Core.Models;

public sealed record InputDocument
{
    public required string PathOrId { get; init; }

    public required string DisplayName { get; init; }

    public long? SizeBytes { get; init; }

    public required int SourceIndex { get; init; }

    public static InputDocument FromPath(string path, int sourceIndex)
    {
        var fileInfo = new FileInfo(path);

        return new InputDocument
        {
            PathOrId = fileInfo.FullName,
            DisplayName = fileInfo.Name,
            SizeBytes = fileInfo.Exists ? fileInfo.Length : null,
            SourceIndex = sourceIndex,
        };
    }
}
