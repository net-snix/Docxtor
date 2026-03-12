namespace Docxtor.Reporting;

public static class OutputFileWriter
{
    public static string CreateTemporarySiblingPath(string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return Path.Combine(
            directory ?? Directory.GetCurrentDirectory(),
            $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
    }

    public static void CommitTemporaryFile(string tempPath, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Move(tempPath, outputPath, overwrite: true);
    }
}
