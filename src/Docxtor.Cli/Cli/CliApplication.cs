using System.Text.Json;
using System.Text.Json.Serialization;
using Docxtor.Core.Abstractions;
using Docxtor.Core.Models;
using Docxtor.Core.Services;
using Docxtor.OpenXml;
using Docxtor.Reporting;

namespace Docxtor.Cli.Cli;

internal static class CliApplication
{
    private static readonly JsonSerializerOptions SummaryJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
    };

    public static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter standardOutput,
        TextWriter standardError,
        string workingDirectory,
        Func<IDocxMerger>? mergerFactory = null,
        CancellationToken cancellationToken = default)
    {
        mergerFactory ??= static () => new DocxtorMerger([new OpenXmlMergeBackend()]);

        if (AppRunCommand.IsMatch(args))
        {
            return await AppRunCommand.RunAsync(
                args,
                standardOutput,
                standardError,
                workingDirectory,
                mergerFactory,
                cancellationToken);
        }

        var parser = new CommandLineParser();
        var (options, parseError) = parser.Parse(args);
        if (parseError is not null)
        {
            await standardError.WriteLineAsync(parseError);
            return ExitCodeMapper.ToExitCode(FailureCode.InvalidArguments);
        }

        if (options is null)
        {
            await standardError.WriteLineAsync("Failed to parse command line.");
            return ExitCodeMapper.ToExitCode(FailureCode.InvalidArguments);
        }

        if (options.ShowHelp)
        {
            await standardOutput.WriteLineAsync(CommandLineParser.BuildHelpText());
            return 0;
        }

        if (options.ShowVersion)
        {
            await standardOutput.WriteLineAsync(typeof(OpenXmlMergeBackend).Assembly.GetName().Version?.ToString() ?? "1.0.0");
            return 0;
        }

        try
        {
            var manifest = new ManifestLoader().Load(options.ConfigPath);
            var (job, resolvedLogFormat, jobError) = new JobFactory().Build(options, manifest, workingDirectory);
            if (jobError is not null || job is null)
            {
                await standardError.WriteLineAsync(jobError ?? "Invalid merge job.");
                return ExitCodeMapper.ToExitCode(FailureCode.InvalidArguments);
            }

            var result = await mergerFactory().MergeAsync(job, cancellationToken: cancellationToken);

            if (job.Validation.EmitReport)
            {
                await new JsonReportWriter().WriteAsync(result.Report, job.ReportPath, cancellationToken);
            }

            await WriteSummaryAsync(standardOutput, standardError, result, job.ReportPath, resolvedLogFormat);
            return ExitCodeMapper.ToExitCode(result.FailureCode);
        }
        catch (Exception ex)
        {
            await standardError.WriteLineAsync(ex.Message);
            return ExitCodeMapper.ToExitCode(FailureCode.InvalidArguments);
        }
    }

    private static async Task WriteSummaryAsync(
        TextWriter standardOutput,
        TextWriter standardError,
        MergeResult result,
        string reportPath,
        LogFormat logFormat)
    {
        if (logFormat == LogFormat.Json)
        {
            var payload = JsonSerializer.Serialize(
                new
                {
                    result.Success,
                    result.OutputPath,
                    result.FailureCode,
                    reportPath,
                    warnings = result.Report.MergeWarnings,
                    errors = result.Report.Errors,
                },
                SummaryJsonOptions);
            await standardOutput.WriteLineAsync(payload);
            return;
        }

        if (result.Success)
        {
            await standardOutput.WriteLineAsync($"Merged DOCX written to {result.OutputPath}");
            await standardOutput.WriteLineAsync($"Report written to {reportPath}");
            return;
        }

        await standardError.WriteLineAsync($"Merge failed: {result.FailureCode}");
        foreach (var error in result.Report.Errors)
        {
            await standardError.WriteLineAsync($"- {error.Code}: {error.Message}");
        }
    }
}
