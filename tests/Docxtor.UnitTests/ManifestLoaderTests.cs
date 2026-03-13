using System.Text;
using Docxtor.Cli.Cli;
using YamlDotNet.Core;

namespace Docxtor.UnitTests;

public sealed class ManifestLoaderTests
{
    [Fact]
    public void Load_parses_json_manifest()
    {
        using var sandbox = new TemporaryDirectory();
        var manifestPath = Path.Combine(sandbox.Path, "manifest.json");
        File.WriteAllText(
            manifestPath,
            """
            {
              "inputs": ["one.docx", "two.docx"],
              "output": "out.docx"
            }
            """);

        var manifest = new ManifestLoader().Load(manifestPath);

        Assert.NotNull(manifest);
        Assert.Equal(["one.docx", "two.docx"], manifest!.Inputs);
        Assert.Equal("out.docx", manifest.Output);
    }

    [Fact]
    public void Load_parses_yaml_manifest()
    {
        using var sandbox = new TemporaryDirectory();
        var manifestPath = Path.Combine(sandbox.Path, "manifest.yaml");
        File.WriteAllText(
            manifestPath,
            """
            inputs:
              - one.docx
              - two.docx
            output: out.docx
            """);

        var manifest = new ManifestLoader().Load(manifestPath);

        Assert.NotNull(manifest);
        Assert.Equal(["one.docx", "two.docx"], manifest!.Inputs);
        Assert.Equal("out.docx", manifest.Output);
    }

    [Fact]
    public void Load_parses_yaml_manifest_with_utf8_bom_and_crlf()
    {
        using var sandbox = new TemporaryDirectory();
        var manifestPath = Path.Combine(sandbox.Path, "manifest.yaml");
        File.WriteAllText(
            manifestPath,
            "inputs:\r\n  - one.docx\r\n  - two.docx\r\noutput: out.docx\r\n",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var manifest = new ManifestLoader().Load(manifestPath);

        Assert.NotNull(manifest);
        Assert.Equal(["one.docx", "two.docx"], manifest!.Inputs);
        Assert.Equal("out.docx", manifest.Output);
    }

    [Fact]
    public void Load_rejects_oversized_manifest()
    {
        using var sandbox = new TemporaryDirectory();
        var manifestPath = Path.Combine(sandbox.Path, "manifest.yaml");
        File.WriteAllText(manifestPath, "inputs:\n  - one.docx\n#");
        using (var stream = new FileStream(manifestPath, FileMode.Append, FileAccess.Write, FileShare.None))
        {
            stream.SetLength((1024 * 1024) + 1);
        }

        var exception = Assert.Throws<InvalidOperationException>(() => new ManifestLoader().Load(manifestPath));
        Assert.Contains("Config file is too large", exception.Message);
    }

    [Fact]
    public void Load_rejects_yaml_with_duplicate_keys()
    {
        using var sandbox = new TemporaryDirectory();
        var manifestPath = Path.Combine(sandbox.Path, "manifest.yaml");
        File.WriteAllText(
            manifestPath,
            """
            output: one.docx
            output: two.docx
            inputs:
              - source.docx
            """);

        Assert.Throws<YamlException>(() => new ManifestLoader().Load(manifestPath));
    }

    [Fact]
    public void Load_rejects_json_with_duplicate_keys()
    {
        using var sandbox = new TemporaryDirectory();
        var manifestPath = Path.Combine(sandbox.Path, "manifest.json");
        File.WriteAllText(
            manifestPath,
            """
            {
              "output": "one.docx",
              "output": "two.docx",
              "inputs": ["source.docx"]
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => new ManifestLoader().Load(manifestPath));
        Assert.Contains("duplicate JSON property 'output'", exception.Message);
    }

    [Fact]
    public void Load_rejects_json_with_case_variant_duplicate_keys_in_nested_objects()
    {
        using var sandbox = new TemporaryDirectory();
        var manifestPath = Path.Combine(sandbox.Path, "manifest.json");
        File.WriteAllText(
            manifestPath,
            """
            {
              "inputs": ["source.docx"],
              "merge": {
                "image_dedup": true,
                "Image_Dedup": false
              }
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => new ManifestLoader().Load(manifestPath));
        Assert.Contains("duplicate JSON property 'Image_Dedup'", exception.Message);
        Assert.Contains("$.merge", exception.Message);
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
