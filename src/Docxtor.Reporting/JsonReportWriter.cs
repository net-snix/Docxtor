using System.Text.Json;
using System.Text.Json.Serialization;
using Docxtor.Core.Models;

namespace Docxtor.Reporting;

public sealed class JsonReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public async Task WriteAsync(MergeReport report, string reportPath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = Path.Combine(
            directory ?? Directory.GetCurrentDirectory(),
            $".{Path.GetFileName(reportPath)}.{Guid.NewGuid():N}.tmp");

        await using var stream = File.Create(tempPath);
        await JsonSerializer.SerializeAsync(stream, report, JsonOptions, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        File.Move(tempPath, reportPath, overwrite: true);
    }
}
