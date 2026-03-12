using Docxtor.Core.Models;

namespace Docxtor.Core.Abstractions;

public interface IMergeBackend
{
    string Name { get; }

    string Version { get; }

    BackendCapabilities GetCapabilities();

    Task<PreflightResult> InspectAsync(
        IReadOnlyList<InputDocument> inputs,
        MergePolicy policy,
        CancellationToken cancellationToken = default);

    Task<MergeResult> MergeAsync(
        MergeJob job,
        CancellationToken cancellationToken = default);

    Task<MergeResult> MergeAsync(
        MergeJob job,
        IProgress<MergeProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
        => MergeAsync(job, cancellationToken);
}
