using Docxtor.Core.Abstractions;
using Docxtor.Core.Models;

namespace Docxtor.Core.Services;

public sealed class DocxtorMerger(IEnumerable<IMergeBackend> backends) : IDocxMerger
{
    private readonly IReadOnlyDictionary<string, IMergeBackend> _backends =
        backends.ToDictionary(backend => backend.Name, StringComparer.OrdinalIgnoreCase);

    public async Task<MergeResult> MergeAsync(
        MergeJob job,
        IProgress<MergeProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new MergeProgressUpdate
        {
            Stage = MergeStage.Starting,
        });

        var backend = ResolveBackend(job.BackendHint);
        if (backend is null)
        {
            return BuildUnavailableBackendResult(job);
        }

        progress?.Report(new MergeProgressUpdate
        {
            Stage = MergeStage.Preflight,
        });

        var preflight = await backend.InspectAsync(job.Inputs, job.Policy, cancellationToken);
        if (!preflight.Success || job.DryRun)
        {
            var result = new MergeResult
            {
                Success = preflight.Success && job.DryRun,
                FailureCode = preflight.Success ? FailureCode.None : preflight.Errors.Any(error => error.Code == "corrupted-input")
                    ? FailureCode.CorruptedOrEncryptedInput
                    : FailureCode.PreflightCapabilityFailure,
                Report = BuildDryRunReport(job, preflight),
            };

            if (result.Success)
            {
                progress?.Report(new MergeProgressUpdate
                {
                    Stage = MergeStage.Completed,
                });
            }

            return result;
        }

        if (backend is IPreflightAwareMergeBackend preflightAwareBackend)
        {
            return await preflightAwareBackend.MergeAsync(job, preflight, progress, cancellationToken);
        }

        return await backend.MergeAsync(job, progress, cancellationToken);
    }

    public async Task<PreflightResult> PreflightAsync(
        MergeJob job,
        CancellationToken cancellationToken = default)
    {
        var backend = ResolveBackend(job.BackendHint);
        if (backend is null)
        {
            return new PreflightResult
            {
                Success = false,
                Backend = job.BackendHint ?? "unknown",
                BackendVersion = "n/a",
                Errors =
                [
                    new DiagnosticMessage
                    {
                        Code = "backend-unavailable",
                        Message = $"Backend '{job.BackendHint}' is not registered.",
                    },
                ],
            };
        }

        return await backend.InspectAsync(job.Inputs, job.Policy, cancellationToken);
    }

    private MergeResult BuildUnavailableBackendResult(MergeJob job)
    {
        var error = new DiagnosticMessage
        {
            Code = "backend-unavailable",
            Message = $"Backend '{job.BackendHint}' is not registered.",
        };

        return new MergeResult
        {
            Success = false,
            FailureCode = FailureCode.BackendUnavailable,
            Report = new MergeReport
            {
                CorrelationId = job.CorrelationId,
                Status = "Failed",
                StartedAtUtc = DateTimeOffset.UtcNow,
                FinishedAtUtc = DateTimeOffset.UtcNow,
                OutputPath = job.OutputPath,
                Backend = job.BackendHint ?? "unknown",
                Policy = job.Policy,
                InputSummaries = job.Inputs,
                Errors = [error],
                FailureCode = FailureCode.BackendUnavailable,
            },
        };
    }

    private static MergeReport BuildDryRunReport(MergeJob job, PreflightResult preflight)
    {
        return new MergeReport
        {
            CorrelationId = job.CorrelationId,
            Status = job.DryRun && preflight.Success ? "DryRun" : "Failed",
            StartedAtUtc = DateTimeOffset.UtcNow,
            FinishedAtUtc = DateTimeOffset.UtcNow,
            OutputPath = job.OutputPath,
            Backend = preflight.Backend,
            Policy = job.Policy,
            InputSummaries = job.Inputs,
            PreflightInventories = preflight.Inputs,
            PreflightWarnings = preflight.Warnings,
            Errors = preflight.Errors,
            FailureCode = preflight.Success ? FailureCode.None : FailureCode.PreflightCapabilityFailure,
        };
    }

    private IMergeBackend? ResolveBackend(string? backendHint)
    {
        if (string.IsNullOrWhiteSpace(backendHint))
        {
            return _backends.Values.FirstOrDefault();
        }

        return _backends.TryGetValue(backendHint, out var backend) ? backend : null;
    }
}
