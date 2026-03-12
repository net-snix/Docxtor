namespace Docxtor.IntegrationTests.Support;

internal sealed class TemporaryTestDirectory : IDisposable
{
    private readonly DirectoryInfo _directory;

    public TemporaryTestDirectory()
    {
        _directory = Directory.CreateTempSubdirectory("docxtor-tests-");
    }

    public string Path => _directory.FullName;

    public void Dispose()
    {
        if (_directory.Exists)
        {
            _directory.Delete(recursive: true);
        }
    }
}
