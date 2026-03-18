using Docxtor.Reporting;

namespace Docxtor.UnitTests;

public sealed class OutputFileWriterTests
{
    [Fact]
    public void CreateTemporarySiblingPath_creates_parent_directory_and_keeps_temp_file_local()
    {
        using var sandbox = new TemporaryDirectory();
        var outputPath = Path.Combine(sandbox.Path, "nested", "merge-report.json");

        var tempPath = OutputFileWriter.CreateTemporarySiblingPath(outputPath);

        Assert.True(Directory.Exists(Path.GetDirectoryName(outputPath)));
        Assert.Equal(Path.GetDirectoryName(outputPath), Path.GetDirectoryName(tempPath));
        Assert.StartsWith(".merge-report.json.", Path.GetFileName(tempPath), StringComparison.Ordinal);
        Assert.EndsWith(".tmp", tempPath, StringComparison.Ordinal);
    }

    [Fact]
    public void CommitTemporaryFile_overwrites_existing_destination()
    {
        using var sandbox = new TemporaryDirectory();
        var outputPath = Path.Combine(sandbox.Path, "merge-report.json");
        var tempPath = OutputFileWriter.CreateTemporarySiblingPath(outputPath);

        File.WriteAllText(outputPath, "old-report");
        File.WriteAllText(tempPath, "new-report");

        OutputFileWriter.CommitTemporaryFile(tempPath, outputPath);

        Assert.False(File.Exists(tempPath));
        Assert.Equal("new-report", File.ReadAllText(outputPath));
    }

    [Fact]
    public void CommitTemporaryFile_creates_parent_directory_for_nested_destination()
    {
        using var sandbox = new TemporaryDirectory();
        var outputPath = Path.Combine(sandbox.Path, "nested", "merge-report.json");
        var tempPath = Path.Combine(sandbox.Path, "staged.json");

        File.WriteAllText(tempPath, "new-report");

        OutputFileWriter.CommitTemporaryFile(tempPath, outputPath);

        Assert.False(File.Exists(tempPath));
        Assert.Equal("new-report", File.ReadAllText(outputPath));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory;

        public TemporaryDirectory()
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
}
