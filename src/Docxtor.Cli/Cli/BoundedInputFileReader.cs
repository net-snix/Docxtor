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

    public static byte[] ReadAllBytes(string path, long maxBytes, string fileLabel)
    {
        using var stream = OpenRead(path, maxBytes, fileLabel);
        var buffer = new byte[stream.Length];
        var totalRead = 0;

        while (totalRead < buffer.Length)
        {
            var bytesRead = stream.Read(buffer, totalRead, buffer.Length - totalRead);
            if (bytesRead == 0)
            {
                break;
            }

            totalRead += bytesRead;
        }

        return totalRead == buffer.Length ? buffer : buffer[..totalRead];
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
