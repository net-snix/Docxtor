using System.Text.Json;
using Docxtor.Cli.Cli;
using Docxtor.Core.Abstractions;
using Docxtor.Core.Models;

namespace Docxtor.UnitTests;

public sealed class CliApplicationTests
{
    [Fact]
    public async Task RunAsync_app_run_emits_success_ndjson_contract()
    {
        using var sandbox = new TemporaryDirectory();
        var requestPath = sandbox.WriteRequest(
            new AppRunRequest
            {
                Inputs = ["docs/one.docx"],
                OutputPath = "out/main.docx",
                ReportPath = "out/main.merge-report.json",
            });

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = await CliApplication.RunAsync(
            ["app-run", "--request", requestPath],
            stdout,
            stderr,
            sandbox.Path,
            () => new FakeMerger(
                progress =>
                {
                    progress?.Report(new MergeProgressUpdate { Stage = MergeStage.Starting });
                    progress?.Report(new MergeProgressUpdate { Stage = MergeStage.Preflight });
                    progress?.Report(new MergeProgressUpdate
                    {
                        Stage = MergeStage.MergingInput,
                        CurrentInputIndex = 1,
                        TotalInputs = 1,
                        InputDisplayName = "one.docx",
                    });
                },
                CreateSuccessResult));

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.True(File.Exists(Path.Combine(sandbox.Path, "out/main.merge-report.json")));

        var events = ParseEvents(stdout);
        var eventTypes = events.Select(@event => @event.GetProperty("type").GetString()!).ToArray();
        Assert.Equal(
            ["started", "stage", "stage", "stage", "stage", "stage", "stage", "completed"],
            eventTypes);
        Assert.Equal("starting", events[1].GetProperty("stage").GetString());
        Assert.Equal("preflight", events[2].GetProperty("stage").GetString());
        Assert.Equal("merging-input", events[3].GetProperty("stage").GetString());
        Assert.Equal(1, events[3].GetProperty("currentInputIndex").GetInt32());
        Assert.Equal("validation", events[4].GetProperty("stage").GetString());
        Assert.Equal("writing-report", events[5].GetProperty("stage").GetString());
        Assert.Equal("completed", events[6].GetProperty("stage").GetString());
        Assert.Equal("None", events[7].GetProperty("failureCode").GetString());
    }

    [Fact]
    public async Task RunAsync_app_run_emits_failed_ndjson_contract()
    {
        using var sandbox = new TemporaryDirectory();
        var requestPath = sandbox.WriteRequest(
            new AppRunRequest
            {
                Inputs = ["docs/one.docx"],
                OutputPath = "out/main.docx",
                ReportPath = "out/main.merge-report.json",
            });

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = await CliApplication.RunAsync(
            ["app-run", "--request", requestPath],
            stdout,
            stderr,
            sandbox.Path,
            () => new FakeMerger(
                progress =>
                {
                    progress?.Report(new MergeProgressUpdate { Stage = MergeStage.Starting });
                    progress?.Report(new MergeProgressUpdate { Stage = MergeStage.Preflight });
                },
                CreateFailureResult));

        Assert.Equal(ExitCodeMapper.ToExitCode(FailureCode.PreflightCapabilityFailure), exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.True(File.Exists(Path.Combine(sandbox.Path, "out/main.merge-report.json")));

        var events = ParseEvents(stdout);
        var eventTypes = events.Select(@event => @event.GetProperty("type").GetString()!).ToArray();
        Assert.Equal(
            ["started", "stage", "stage", "stage", "failed"],
            eventTypes);
        Assert.Equal("writing-report", events[3].GetProperty("stage").GetString());
        Assert.Equal("PreflightCapabilityFailure", events[4].GetProperty("failureCode").GetString());
        Assert.Equal("Tracked changes are not supported.", events[4].GetProperty("message").GetString());
        Assert.Equal(
            "preflight-failed",
            events[4].GetProperty("errors").EnumerateArray().First().GetProperty("code").GetString());
    }

    [Fact]
    public async Task RunAsync_app_run_rejects_oversized_request_file()
    {
        using var sandbox = new TemporaryDirectory();
        var requestPath = sandbox.WriteOversizedRequest();

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = await CliApplication.RunAsync(
            ["app-run", "--request", requestPath],
            stdout,
            stderr,
            sandbox.Path,
            () => new FakeMerger(_ => { }, CreateSuccessResult));

        Assert.Equal(ExitCodeMapper.ToExitCode(FailureCode.InvalidArguments), exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        var events = ParseEvents(stdout);
        Assert.Single(events);
        Assert.Equal("failed", events[0].GetProperty("type").GetString());
        Assert.Equal("InvalidArguments", events[0].GetProperty("failureCode").GetString());
        Assert.Equal(
            "invalid-request",
            events[0].GetProperty("errors").EnumerateArray().First().GetProperty("code").GetString());
        Assert.Contains("Request file is too large", events[0].GetProperty("message").GetString());
    }

    private static MergeResult CreateSuccessResult(MergeJob job)
    {
        return new MergeResult
        {
            Success = true,
            OutputPath = job.OutputPath,
            FailureCode = FailureCode.None,
            Report = new MergeReport
            {
                CorrelationId = job.CorrelationId,
                Status = "Success",
                StartedAtUtc = DateTimeOffset.UtcNow,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                OutputPath = job.OutputPath,
                Backend = job.BackendHint ?? "openxml-sdk",
                Policy = job.Policy,
                InputSummaries = job.Inputs,
            },
        };
    }

    private static MergeResult CreateFailureResult(MergeJob job)
    {
        return new MergeResult
        {
            Success = false,
            OutputPath = job.OutputPath,
            FailureCode = FailureCode.PreflightCapabilityFailure,
            Report = new MergeReport
            {
                CorrelationId = job.CorrelationId,
                Status = "Failed",
                StartedAtUtc = DateTimeOffset.UtcNow,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                OutputPath = job.OutputPath,
                Backend = job.BackendHint ?? "openxml-sdk",
                Policy = job.Policy,
                InputSummaries = job.Inputs,
                Errors =
                [
                    new DiagnosticMessage
                    {
                        Code = "preflight-failed",
                        Message = "Tracked changes are not supported.",
                    },
                ],
                FailureCode = FailureCode.PreflightCapabilityFailure,
            },
        };
    }

    private static List<JsonElement> ParseEvents(StringWriter stdout)
    {
        return stdout
            .ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonDocument.Parse(line).RootElement.Clone())
            .ToList();
    }

    private sealed class FakeMerger(
        Action<IProgress<MergeProgressUpdate>?> onMerge,
        Func<MergeJob, MergeResult> resultFactory) : IDocxMerger
    {
        public Task<MergeResult> MergeAsync(
            MergeJob job,
            IProgress<MergeProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            onMerge(progress);
            return Task.FromResult(resultFactory(job));
        }

        public Task<PreflightResult> PreflightAsync(MergeJob job, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"docxtor-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string WriteRequest(AppRunRequest request)
        {
            var requestPath = System.IO.Path.Combine(Path, "request.json");
            File.WriteAllText(requestPath, JsonSerializer.Serialize(request));
            return requestPath;
        }

        public string WriteOversizedRequest()
        {
            const int maxBytes = 1 * 1024 * 1024;
            var requestPath = System.IO.Path.Combine(Path, "request.json");
            var content = new string('x', maxBytes + 1);
            File.WriteAllText(requestPath, content);
            return requestPath;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
