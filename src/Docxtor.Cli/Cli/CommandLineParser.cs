using Docxtor.Core.Models;

namespace Docxtor.Cli.Cli;

internal sealed class CommandLineParser
{
    private delegate bool TryParseOption<TValue>(string? value, out TValue parsedValue);

    public (CommandLineOptions? Options, string? Error) Parse(IReadOnlyList<string> args)
    {
        var options = new CommandLineOptions();
        var inputs = new List<string>();

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            if (index == 0 && StringComparer.OrdinalIgnoreCase.Equals(argument, "merge"))
            {
                continue;
            }

            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                inputs.Add(argument);
                continue;
            }

            switch (argument)
            {
                case "--help":
                    options = options with { ShowHelp = true };
                    break;
                case "--version":
                    options = options with { ShowVersion = true };
                    break;
                case "--dry-run":
                    options = options with { DryRun = true };
                    break;
                case "--no-report":
                    options = options with { EmitReport = false };
                    break;
                case "--config":
                case "--manifest":
                    if (!TryReadRequiredValue(args, ref index, argument, out var configPath, out var configError))
                    {
                        return (null, configError);
                    }

                    options = options with { ConfigPath = configPath };
                    break;
                case "--output":
                    if (!TryReadRequiredValue(args, ref index, argument, out var outputPath, out var outputError))
                    {
                        return (null, outputError);
                    }

                    options = options with { OutputPath = outputPath };
                    break;
                case "--report":
                    if (!TryReadRequiredValue(args, ref index, argument, out var reportPath, out var reportError))
                    {
                        return (null, reportError);
                    }

                    options = options with { ReportPath = reportPath, EmitReport = true };
                    break;
                case "--template":
                    if (!TryReadRequiredValue(args, ref index, argument, out var templatePath, out var templateError))
                    {
                        return (null, templateError);
                    }

                    options = options with { TemplatePath = templatePath };
                    break;
                case "--backend":
                    if (!TryReadRequiredValue(args, ref index, argument, out var backend, out var backendError))
                    {
                        return (null, backendError);
                    }

                    options = options with { Backend = backend };
                    break;
                case "--boundary":
                    if (!TryReadParsedOption<BoundaryMode>(
                        args,
                        ref index,
                        argument,
                        "boundary mode",
                        MergeOptionParsers.TryParseBoundaryMode,
                        out var boundaryMode,
                        out var boundaryError))
                    {
                        return (null, boundaryError);
                    }

                    options = options with { BoundaryMode = boundaryMode };
                    break;
                case "--numbering":
                    if (!TryReadParsedOption<NumberingMode>(
                        args,
                        ref index,
                        argument,
                        "numbering mode",
                        MergeOptionParsers.TryParseNumberingMode,
                        out var numberingMode,
                        out var numberingError))
                    {
                        return (null, numberingError);
                    }

                    options = options with { NumberingMode = numberingMode };
                    break;
                case "--tracked-changes":
                    if (!TryReadParsedOption<TrackedChangesMode>(
                        args,
                        ref index,
                        argument,
                        "tracked-changes mode",
                        MergeOptionParsers.TryParseTrackedChangesMode,
                        out var trackedChangesMode,
                        out var trackedChangesError))
                    {
                        return (null, trackedChangesError);
                    }

                    options = options with { TrackedChangesMode = trackedChangesMode };
                    break;
                case "--altchunk":
                    if (!TryReadParsedOption<AltChunkMode>(
                        args,
                        ref index,
                        argument,
                        "altchunk mode",
                        MergeOptionParsers.TryParseAltChunkMode,
                        out var altChunkMode,
                        out var altChunkError))
                    {
                        return (null, altChunkError);
                    }

                    options = options with { AltChunkMode = altChunkMode };
                    break;
                case "--theme-policy":
                    if (!TryReadParsedOption<ThemePolicy>(
                        args,
                        ref index,
                        argument,
                        "theme policy",
                        MergeOptionParsers.TryParseThemePolicy,
                        out var themePolicy,
                        out var themeError))
                    {
                        return (null, themeError);
                    }

                    options = options with { ThemePolicy = themePolicy };
                    break;
                case "--external-resources":
                    if (!TryReadParsedOption<ExternalResourceMode>(
                        args,
                        ref index,
                        argument,
                        "external-resource mode",
                        MergeOptionParsers.TryParseExternalResourceMode,
                        out var externalMode,
                        out var externalError))
                    {
                        return (null, externalError);
                    }

                    options = options with { ExternalResourceMode = externalMode };
                    break;
                case "--log-format":
                    if (!TryReadParsedOption<LogFormat>(
                        args,
                        ref index,
                        argument,
                        "log format",
                        MergeOptionParsers.TryParseLogFormat,
                        out var logFormat,
                        out var logFormatError))
                    {
                        return (null, logFormatError);
                    }

                    options = options with { LogFormat = logFormat };
                    break;
                case "--preserve-sections":
                    options = options with
                    {
                        PreserveSections = ReadOptionalBoolean(args, ref index),
                    };
                    break;
                case "--preserve-headers-footers":
                    options = options with
                    {
                        PreserveHeadersFooters = ReadOptionalBoolean(args, ref index),
                    };
                    break;
                case "--image-dedup":
                    options = options with { ImageDeduplication = ReadOptionalBoolean(args, ref index) };
                    break;
                case "--update-fields-on-open":
                    options = options with { UpdateFieldsOnOpen = ReadOptionalBoolean(args, ref index) };
                    break;
                case "--validate-openxml":
                    options = options with { ValidateOpenXml = ReadOptionalBoolean(args, ref index) };
                    break;
                case "--validate-references":
                    options = options with { ValidateReferences = ReadOptionalBoolean(args, ref index) };
                    break;
                case "--visual-qa":
                    options = options with { VisualQa = ReadOptionalBoolean(args, ref index) };
                    break;
                case "--fail-on-warnings":
                    options = options with { FailOnWarnings = ReadOptionalBoolean(args, ref index) };
                    break;
                default:
                    return (null, $"Unknown option '{argument}'.");
            }
        }

        options = options with { Inputs = inputs };
        return (options, null);
    }

    public static string BuildHelpText()
    {
        return """
            Docxtor - high-fidelity DOCX merger

            Usage:
              docxtor merge [options] input1.docx input2.docx ...
              docxtor [options] input1.docx input2.docx ...

            Options:
              --output <path>
              --config <path>
              --template <path>
              --backend <name>
              --boundary <section-new-page|page-break|continuous-section|none>
              --preserve-sections [true|false]
              --preserve-headers-footers [true|false]
              --numbering <preserve-source|continue-destination>
              --tracked-changes <fail|accept-all|reject-all>
              --altchunk <reject|resolve>
              --theme-policy <base-wins|import-first|template-wins>
              --external-resources <preserve-links|materialize>
              --image-dedup [true|false]
              --update-fields-on-open [true|false]
              --validate-openxml [true|false]
              --validate-references [true|false]
              --visual-qa [true|false]
              --report <path>
              --no-report
              --log-format <text|json>
              --fail-on-warnings [true|false]
              --dry-run
              --version
              --help
            """;
    }

    private static bool TryReadRequiredValue(
        IReadOnlyList<string> args,
        ref int index,
        string optionName,
        out string? value,
        out string? error)
    {
        error = null;
        if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            error = $"Option '{optionName}' requires a value.";
            value = null;
            return false;
        }

        index++;
        value = args[index];
        return true;
    }

    private static bool ReadOptionalBoolean(IReadOnlyList<string> args, ref int index)
    {
        if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            return true;
        }

        if (!bool.TryParse(args[index + 1], out var value))
        {
            return true;
        }

        index++;
        return value;
    }

    private static bool TryReadParsedOption<TValue>(
        IReadOnlyList<string> args,
        ref int index,
        string optionName,
        string optionLabel,
        TryParseOption<TValue> tryParse,
        out TValue value,
        out string? error)
    {
        if (!TryReadRequiredValue(args, ref index, optionName, out var rawValue, out error))
        {
            value = default!;
            return false;
        }

        if (!tryParse(rawValue, out value))
        {
            error = $"Unknown {optionLabel} '{rawValue}'.";
            return false;
        }

        return true;
    }
}
