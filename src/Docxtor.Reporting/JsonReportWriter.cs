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
        var tempPath = OutputFileWriter.CreateTemporarySiblingPath(reportPath);

        await using var stream = File.Create(tempPath);
        await JsonSerializer.SerializeAsync(stream, report, JsonOptions, cancellationToken);
        await stream.FlushAsync(cancellationToken);

        OutputFileWriter.CommitTemporaryFile(tempPath, reportPath);
    }
}
