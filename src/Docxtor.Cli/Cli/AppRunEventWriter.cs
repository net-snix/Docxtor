using System.Text.Json;
using System.Text.Json.Serialization;
using Docxtor.Core.Models;

namespace Docxtor.Cli.Cli;

internal sealed class AppRunEventWriter(TextWriter writer)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public void WriteStarted(MergeJob job)
    {
        Write(new
        {
            type = "started",
            correlationId = job.CorrelationId,
            outputPath = job.OutputPath,
            reportPath = job.ReportPath,
            inputCount = job.Inputs.Count,
            message = "Merge started.",
        });
    }

    public void WriteStage(MergeProgressUpdate update)
    {
        Write(new
        {
            type = "stage",
            stage = ToWireName(update.Stage),
            currentInputIndex = update.CurrentInputIndex,
            totalInputs = update.TotalInputs,
            inputDisplayName = update.InputDisplayName,
        });
    }

    public void WriteCompleted(MergeResult result, string reportPath)
    {
        Write(new
        {
            type = "completed",
            correlationId = result.Report.CorrelationId,
            outputPath = result.OutputPath,
            reportPath,
            failureCode = result.FailureCode,
        });
    }

    public void WriteFailed(
        FailureCode failureCode,
        IReadOnlyList<DiagnosticMessage> errors,
        string? correlationId = null,
        string? outputPath = null,
        string? reportPath = null)
    {
        var message = errors.FirstOrDefault()?.Message ?? failureCode.ToString();

        Write(new
        {
            type = "failed",
            correlationId,
            outputPath,
            reportPath,
            message,
            failureCode,
            errors,
        });
    }

    private void Write(object payload)
    {
        writer.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        writer.Flush();
    }

    private static string ToWireName(MergeStage stage) => stage switch
    {
        MergeStage.Starting => "starting",
        MergeStage.Preflight => "preflight",
        MergeStage.MergingInput => "merging-input",
        MergeStage.Validation => "validation",
        MergeStage.WritingReport => "writing-report",
        MergeStage.Completed => "completed",
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown merge stage."),
    };
}
