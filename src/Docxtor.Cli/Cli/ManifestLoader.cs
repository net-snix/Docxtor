using System.Text.Json;
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
        EnsureManifestWithinSizeLimit(fullPath);

        return Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".json" => DeserializeJson(fullPath),
            ".yaml" or ".yml" => DeserializeYaml(fullPath),
            _ => throw new InvalidOperationException("Config file must be JSON or YAML."),
        };
    }

    private static ManifestFileModel? DeserializeJson(string fullPath)
    {
        using var stream = File.OpenRead(fullPath);
        return JsonSerializer.Deserialize<ManifestFileModel>(stream, JsonOptions);
    }

    private ManifestFileModel? DeserializeYaml(string fullPath)
    {
        var content = File.ReadAllText(fullPath);
        return _yamlDeserializer.Deserialize<ManifestFileModel>(content);
    }

    private static void EnsureManifestWithinSizeLimit(string fullPath)
    {
        var manifestLength = new FileInfo(fullPath).Length;
        if (manifestLength <= MaxManifestSizeBytes)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Config file is too large ({manifestLength} bytes). Maximum supported size is {MaxManifestSizeBytes} bytes.");
    }
}
