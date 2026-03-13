using System.Text.Json;
using Docxtor.Core.Abstractions;
using Docxtor.Core.Models;
using Docxtor.Reporting;

namespace Docxtor.Cli.Cli;

internal static class AppRunCommand
{
    private const long MaxRequestSizeBytes = 1 * 1024 * 1024;

    private static readonly JsonSerializerOptions RequestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static bool IsMatch(IReadOnlyList<string> args)
        => args.Count > 0 && StringComparer.OrdinalIgnoreCase.Equals(args[0], "app-run");

    public static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter standardOutput,
        TextWriter standardError,
        string workingDirectory,
        Func<IDocxMerger> mergerFactory,
        CancellationToken cancellationToken = default)
    {
        var eventWriter = new AppRunEventWriter(standardOutput);
        try
        {
            var (requestPath, parseError) = Parse(args, workingDirectory);
            if (parseError is not null)
            {
                eventWriter.WriteFailed(
                    FailureCode.InvalidArguments,
                    [CreateError("invalid-arguments", parseError)]);
                return ExitCodeMapper.ToExitCode(FailureCode.InvalidArguments);
            }

            var (request, loadError) = await LoadRequestAsync(requestPath!, cancellationToken);
            if (loadError is not null)
            {
                eventWriter.WriteFailed(
                    FailureCode.InvalidArguments,
                    [CreateError("invalid-request", loadError)]);
                return ExitCodeMapper.ToExitCode(FailureCode.InvalidArguments);
            }

            var requestDirectory = Path.GetDirectoryName(requestPath!) ?? workingDirectory;
            var (job, buildError) = new AppRunJobFactory().Build(request, requestDirectory);
            if (buildError is not null || job is null)
            {
                eventWriter.WriteFailed(
                    FailureCode.InvalidArguments,
                    [CreateError("invalid-request", buildError ?? "Invalid app-run request.")]);
                return ExitCodeMapper.ToExitCode(FailureCode.InvalidArguments);
            }

            eventWriter.WriteStarted(job);

            var sawMergeStage = false;
            var progress = new InlineProgress<MergeProgressUpdate>(update =>
            {
                if (update.Stage == MergeStage.MergingInput)
                {
                    sawMergeStage = true;
                }

                eventWriter.WriteStage(update);
            });

            var result = await mergerFactory().MergeAsync(job, progress, cancellationToken);

            if (sawMergeStage)
            {
                eventWriter.WriteStage(new MergeProgressUpdate
                {
                    Stage = MergeStage.Validation,
                });
            }

            if (job.Validation.EmitReport)
            {
                eventWriter.WriteStage(new MergeProgressUpdate
                {
                    Stage = MergeStage.WritingReport,
                });

                try
                {
                    await new JsonReportWriter().WriteAsync(result.Report, job.ReportPath, cancellationToken);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    eventWriter.WriteFailed(
                        FailureCode.OutputWriteFailure,
                        [CreateError("report-write-failed", ex.Message)],
                        result.Report.CorrelationId,
                        result.OutputPath,
                        job.ReportPath);
                    return ExitCodeMapper.ToExitCode(FailureCode.OutputWriteFailure);
                }
            }

            if (result.Success)
            {
                eventWriter.WriteStage(new MergeProgressUpdate
                {
                    Stage = MergeStage.Completed,
                });
                eventWriter.WriteCompleted(result, job.ReportPath);
                return ExitCodeMapper.ToExitCode(result.FailureCode);
            }

            eventWriter.WriteFailed(
                result.FailureCode,
                result.Report.Errors,
                result.Report.CorrelationId,
                result.OutputPath,
                job.ReportPath);
            return ExitCodeMapper.ToExitCode(result.FailureCode);
        }
        catch (Exception ex)
        {
            await standardError.WriteLineAsync(ex.Message);
            eventWriter.WriteFailed(
                FailureCode.InvalidArguments,
                [CreateError("app-run-crash", ex.Message)]);
            return ExitCodeMapper.ToExitCode(FailureCode.InvalidArguments);
        }
    }

    private static (string? RequestPath, string? Error) Parse(IReadOnlyList<string> args, string workingDirectory)
    {
        string? requestPath = null;

        for (var index = 1; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--request":
                    if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        return (null, "Option '--request' requires a value.");
                    }

                    requestPath = Path.GetFullPath(args[++index], workingDirectory);
                    break;
                default:
                    return (null, $"Unknown option '{argument}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(requestPath))
        {
            return (null, "Option '--request' is required.");
        }

        return (requestPath, null);
    }

    private static async Task<(AppRunRequest? Request, string? Error)> LoadRequestAsync(
        string requestPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = BoundedInputFileReader.OpenRead(requestPath, MaxRequestSizeBytes, "Request file");
            var request = await JsonSerializer.DeserializeAsync<AppRunRequest>(
                stream,
                RequestJsonOptions,
                cancellationToken);
            return (request, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            return (null, ex.Message);
        }
    }

    private static DiagnosticMessage CreateError(string code, string message)
        => new()
        {
            Code = code,
            Message = message,
        };

    private sealed class InlineProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
