using System.Text.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Docxtor.Cli.Cli;

internal sealed class ManifestLoader
{
    private const long MaxManifestSizeBytes = 1 * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .WithDuplicateKeyChecking()
        .IgnoreUnmatchedProperties()
        .Build();

    public ManifestFileModel? Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(path);

        return Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".json" => DeserializeJson(fullPath),
            ".yaml" or ".yml" => DeserializeYaml(fullPath),
            _ => throw new InvalidOperationException("Config file must be JSON or YAML."),
        };
    }

    private static ManifestFileModel? DeserializeJson(string fullPath)
    {
        using var stream = BoundedInputFileReader.OpenRead(fullPath, MaxManifestSizeBytes, "Config file");
        return JsonSerializer.Deserialize<ManifestFileModel>(stream, JsonOptions);
    }

    private ManifestFileModel? DeserializeYaml(string fullPath)
    {
        using var stream = BoundedInputFileReader.OpenRead(fullPath, MaxManifestSizeBytes, "Config file");
        using var reader = new StreamReader(stream);
        return (ManifestFileModel?)_yamlDeserializer.Deserialize(new Parser(reader), typeof(ManifestFileModel));
    }
}
