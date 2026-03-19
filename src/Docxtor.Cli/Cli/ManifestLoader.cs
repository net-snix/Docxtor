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
        var extension = Path.GetExtension(fullPath);

        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            return DeserializeJson(fullPath);
        }

        if (extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase) || extension.Equals(".yml", StringComparison.OrdinalIgnoreCase))
        {
            return DeserializeYaml(fullPath);
        }

        throw new InvalidOperationException("Config file must be JSON or YAML.");
    }

    private static ManifestFileModel? DeserializeJson(string fullPath)
    {
        var json = BoundedInputFileReader.ReadAllBytes(fullPath, MaxManifestSizeBytes, "Config file");
        EnsureNoDuplicateJsonProperties(json);
        return JsonSerializer.Deserialize<ManifestFileModel>(json, JsonOptions);
    }

    private ManifestFileModel? DeserializeYaml(string fullPath)
    {
        using var stream = BoundedInputFileReader.OpenRead(fullPath, MaxManifestSizeBytes, "Config file");
        using var reader = new StreamReader(stream);
        return (ManifestFileModel?)_yamlDeserializer.Deserialize(new Parser(reader), typeof(ManifestFileModel));
    }

    private static void EnsureNoDuplicateJsonProperties(byte[] json)
    {
        using var document = JsonDocument.Parse(json);
        EnsureNoDuplicateJsonProperties(document.RootElement, "$");
    }

    private static void EnsureNoDuplicateJsonProperties(JsonElement element, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in element.EnumerateObject())
                {
                    if (!propertyNames.Add(property.Name))
                    {
                        throw new InvalidOperationException(
                            $"Config file contains duplicate JSON property '{property.Name}' at '{path}'.");
                    }

                    EnsureNoDuplicateJsonProperties(property.Value, $"{path}.{property.Name}");
                }

                break;
            }
            case JsonValueKind.Array:
            {
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    EnsureNoDuplicateJsonProperties(item, $"{path}[{index}]");
                    index++;
                }

                break;
            }
        }
    }
}
