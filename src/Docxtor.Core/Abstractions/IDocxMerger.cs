using Docxtor.Core.Models;

namespace Docxtor.Core.Abstractions;

public interface IDocxMerger
{
    Task<MergeResult> MergeAsync(
        MergeJob job,
        IProgress<MergeProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);

    Task<PreflightResult> PreflightAsync(MergeJob job, CancellationToken cancellationToken = default);
}
