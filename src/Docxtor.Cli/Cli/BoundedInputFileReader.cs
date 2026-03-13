namespace Docxtor.Cli.Cli;

internal static class BoundedInputFileReader
{
    public static FileStream OpenRead(string path, long maxBytes, string fileLabel)
    {
        var stream = new FileStream(
            path,
            new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.Read,
                Options = FileOptions.SequentialScan,
            });

        try
        {
            EnsureWithinSizeLimit(stream.Length, maxBytes, fileLabel);
            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public static string ReadAllText(string path, long maxBytes, string fileLabel)
    {
        using var stream = OpenRead(path, maxBytes, fileLabel);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void EnsureWithinSizeLimit(long length, long maxBytes, string fileLabel)
    {
        if (length <= maxBytes)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{fileLabel} is too large ({length} bytes). Maximum supported size is {maxBytes} bytes.");
    }
}
