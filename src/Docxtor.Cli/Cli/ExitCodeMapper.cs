using Docxtor.Core.Models;

namespace Docxtor.Cli.Cli;

internal static class ExitCodeMapper
{
    public static int ToExitCode(FailureCode failureCode)
    {
        return failureCode switch
        {
            FailureCode.None => 0,
            FailureCode.InvalidArguments => 2,
            FailureCode.PreflightCapabilityFailure => 3,
            FailureCode.ValidationFailure => 4,
            FailureCode.OutputWriteFailure => 5,
            FailureCode.CorruptedOrEncryptedInput => 6,
            FailureCode.BackendUnavailable => 7,
            _ => 1,
        };
    }
}
