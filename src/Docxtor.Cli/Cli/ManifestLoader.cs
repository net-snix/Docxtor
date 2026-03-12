using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Docxtor.Cli.Cli;

internal sealed class ManifestLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public ManifestFileModel? Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(path);
        var content = File.ReadAllText(fullPath);
        return Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".json" => JsonSerializer.Deserialize<ManifestFileModel>(content, JsonOptions),
            ".yaml" or ".yml" => _yamlDeserializer.Deserialize<ManifestFileModel>(content),
            _ => throw new InvalidOperationException("Config file must be JSON or YAML."),
        };
    }
}
