namespace Docxtor.Reporting;

public static class OutputFileWriter
{
    public static string CreateTemporarySiblingPath(string outputPath)
    {
        EnsureParentDirectoryExists(outputPath);
        var directory = Path.GetDirectoryName(outputPath);
        return Path.Combine(
            directory ?? Directory.GetCurrentDirectory(),
            $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
    }

    public static void CommitTemporaryFile(string tempPath, string outputPath)
    {
        EnsureParentDirectoryExists(outputPath);
        File.Move(tempPath, outputPath, overwrite: true);
    }

    private static void EnsureParentDirectoryExists(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
