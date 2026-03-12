using Docxtor.Core.Abstractions;
using Docxtor.Core.Models;
using Docxtor.Core.Services;

namespace Docxtor.UnitTests;

public sealed class DocxtorMergerTests
{
    [Fact]
    public async Task MergeAsync_returns_backend_unavailable_when_hint_does_not_match()
    {
        var merger = new DocxtorMerger([new FakeMergeBackend()]);

        var result = await merger.MergeAsync(new MergeJob
        {
            Inputs = [InputDocument.FromPath("/tmp/one.docx", 0)],
            BackendHint = "missing-backend",
        });

        Assert.False(result.Success);
        Assert.Equal(FailureCode.BackendUnavailable, result.FailureCode);
    }

    [Fact]
    public async Task MergeAsync_returns_dry_run_success_without_invoking_backend_merge()
    {
        var backend = new FakeMergeBackend();
        var merger = new DocxtorMerger([backend]);

        var result = await merger.MergeAsync(new MergeJob
        {
            Inputs = [InputDocument.FromPath("/tmp/one.docx", 0)],
            DryRun = true,
            BackendHint = backend.Name,
        });

        Assert.True(result.Success);
        Assert.Equal("DryRun", result.Report.Status);
        Assert.Equal(0, backend.MergeCalls);
    }

    [Fact]
    public async Task MergeAsync_reports_progress_in_expected_order()
    {
        var backend = new FakeMergeBackend();
        var merger = new DocxtorMerger([backend]);
        var updates = new List<MergeProgressUpdate>();

        await merger.MergeAsync(
            new MergeJob
            {
                Inputs =
                [
                    InputDocument.FromPath("/tmp/one.docx", 0),
                    InputDocument.FromPath("/tmp/two.docx", 1),
                ],
                BackendHint = backend.Name,
            },
            new InlineProgress<MergeProgressUpdate>(updates.Add));

        Assert.Collection(
            updates,
            update => Assert.Equal(MergeStage.Starting, update.Stage),
            update => Assert.Equal(MergeStage.Preflight, update.Stage),
            update =>
            {
                Assert.Equal(MergeStage.MergingInput, update.Stage);
                Assert.Equal(1, update.CurrentInputIndex);
                Assert.Equal(2, update.TotalInputs);
                Assert.Equal("one.docx", update.InputDisplayName);
            },
            update =>
            {
                Assert.Equal(MergeStage.MergingInput, update.Stage);
                Assert.Equal(2, update.CurrentInputIndex);
                Assert.Equal(2, update.TotalInputs);
                Assert.Equal("two.docx", update.InputDisplayName);
            });
    }

    private sealed class FakeMergeBackend : IMergeBackend
    {
        public int MergeCalls { get; private set; }

        public string Name => "fake";

        public string Version => "1.0.0";

        public BackendCapabilities GetCapabilities() => new();

        public Task<PreflightResult> InspectAsync(IReadOnlyList<InputDocument> inputs, MergePolicy policy, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PreflightResult
            {
                Success = true,
                Backend = Name,
                BackendVersion = Version,
                Inputs = inputs.Select(input => new FeatureInventory
                {
                    InputPath = input.PathOrId,
                }).ToArray(),
            });
        }

        public Task<MergeResult> MergeAsync(MergeJob job, CancellationToken cancellationToken = default)
            => MergeAsync(job, progress: null, cancellationToken);

        public Task<MergeResult> MergeAsync(
            MergeJob job,
            IProgress<MergeProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            MergeCalls++;

            foreach (var input in job.Inputs.OrderBy(input => input.SourceIndex))
            {
                progress?.Report(new MergeProgressUpdate
                {
                    Stage = MergeStage.MergingInput,
                    CurrentInputIndex = input.SourceIndex + 1,
                    TotalInputs = job.Inputs.Count,
                    InputDisplayName = input.DisplayName,
                });
            }

            return Task.FromResult(new MergeResult
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
                    Backend = Name,
                    Policy = job.Policy,
                    InputSummaries = job.Inputs,
                },
            });
        }
    }

    private sealed class InlineProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
