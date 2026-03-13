using Docxtor.Core.Models;

namespace Docxtor.Core.Abstractions;

public interface IPreflightAwareMergeBackend : IMergeBackend
{
    Task<MergeResult> MergeAsync(
        MergeJob job,
        PreflightResult preflight,
        IProgress<MergeProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);
}
