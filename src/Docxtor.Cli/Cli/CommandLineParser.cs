using Docxtor.Core.Models;

namespace Docxtor.Cli.Cli;

internal sealed class CommandLineParser
{
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
                    options = options with { ConfigPath = ReadRequiredValue(args, ref index, argument, out var configError) };
                    if (configError is not null)
                    {
                        return (null, configError);
                    }

                    break;
                case "--output":
                    options = options with { OutputPath = ReadRequiredValue(args, ref index, argument, out var outputError) };
                    if (outputError is not null)
                    {
                        return (null, outputError);
                    }

                    break;
                case "--report":
                    options = options with { ReportPath = ReadRequiredValue(args, ref index, argument, out var reportError), EmitReport = true };
                    if (reportError is not null)
                    {
                        return (null, reportError);
                    }

                    break;
                case "--template":
                    options = options with { TemplatePath = ReadRequiredValue(args, ref index, argument, out var templateError) };
                    if (templateError is not null)
                    {
                        return (null, templateError);
                    }

                    break;
                case "--backend":
                    options = options with { Backend = ReadRequiredValue(args, ref index, argument, out var backendError) };
                    if (backendError is not null)
                    {
                        return (null, backendError);
                    }

                    break;
                case "--boundary":
                    var boundaryValue = ReadRequiredValue(args, ref index, argument, out var boundaryError);
                    if (boundaryError is not null)
                    {
                        return (null, boundaryError);
                    }

                    if (!TryParseBoundaryMode(boundaryValue, out var boundaryMode))
                    {
                        return (null, $"Unknown boundary mode '{boundaryValue}'.");
                    }

                    options = options with { BoundaryMode = boundaryMode };
                    break;
                case "--numbering":
                    var numberingValue = ReadRequiredValue(args, ref index, argument, out var numberingError);
                    if (numberingError is not null)
                    {
                        return (null, numberingError);
                    }

                    if (!TryParseNumberingMode(numberingValue, out var numberingMode))
                    {
                        return (null, $"Unknown numbering mode '{numberingValue}'.");
                    }

                    options = options with { NumberingMode = numberingMode };
                    break;
                case "--tracked-changes":
                    var trackedChangesValue = ReadRequiredValue(args, ref index, argument, out var trackedChangesError);
                    if (trackedChangesError is not null)
                    {
                        return (null, trackedChangesError);
                    }

                    if (!TryParseTrackedChangesMode(trackedChangesValue, out var trackedChangesMode))
                    {
                        return (null, $"Unknown tracked-changes mode '{trackedChangesValue}'.");
                    }

                    options = options with { TrackedChangesMode = trackedChangesMode };
                    break;
                case "--altchunk":
                    var altChunkValue = ReadRequiredValue(args, ref index, argument, out var altChunkError);
                    if (altChunkError is not null)
                    {
                        return (null, altChunkError);
                    }

                    if (!TryParseAltChunkMode(altChunkValue, out var altChunkMode))
                    {
                        return (null, $"Unknown altchunk mode '{altChunkValue}'.");
                    }

                    options = options with { AltChunkMode = altChunkMode };
                    break;
                case "--theme-policy":
                    var themeValue = ReadRequiredValue(args, ref index, argument, out var themeError);
                    if (themeError is not null)
                    {
                        return (null, themeError);
                    }

                    if (!TryParseThemePolicy(themeValue, out var themePolicy))
                    {
                        return (null, $"Unknown theme policy '{themeValue}'.");
                    }

                    options = options with { ThemePolicy = themePolicy };
                    break;
                case "--external-resources":
                    var externalValue = ReadRequiredValue(args, ref index, argument, out var externalError);
                    if (externalError is not null)
                    {
                        return (null, externalError);
                    }

                    if (!TryParseExternalResourceMode(externalValue, out var externalMode))
                    {
                        return (null, $"Unknown external-resource mode '{externalValue}'.");
                    }

                    options = options with { ExternalResourceMode = externalMode };
                    break;
                case "--log-format":
                    var logFormatValue = ReadRequiredValue(args, ref index, argument, out var logFormatError);
                    if (logFormatError is not null)
                    {
                        return (null, logFormatError);
                    }

                    if (!TryParseLogFormat(logFormatValue, out var logFormat))
                    {
                        return (null, $"Unknown log format '{logFormatValue}'.");
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

    private static string? ReadRequiredValue(IReadOnlyList<string> args, ref int index, string optionName, out string? error)
    {
        error = null;
        if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            error = $"Option '{optionName}' requires a value.";
            return null;
        }

        index++;
        return args[index];
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

    private static bool TryParseBoundaryMode(string? value, out BoundaryMode mode)
    {
        mode = value switch
        {
            "section-new-page" => BoundaryMode.SectionNewPage,
            "page-break" => BoundaryMode.PageBreak,
            "continuous-section" => BoundaryMode.ContinuousSection,
            "none" => BoundaryMode.None,
            _ => default,
        };

        return value is "section-new-page" or "page-break" or "continuous-section" or "none";
    }

    private static bool TryParseNumberingMode(string? value, out NumberingMode mode)
    {
        mode = value switch
        {
            "preserve-source" => NumberingMode.PreserveSource,
            "continue-destination" => NumberingMode.ContinueDestination,
            _ => default,
        };

        return value is "preserve-source" or "continue-destination";
    }

    private static bool TryParseTrackedChangesMode(string? value, out TrackedChangesMode mode)
    {
        mode = value switch
        {
            "fail" => TrackedChangesMode.Fail,
            "accept-all" => TrackedChangesMode.AcceptAll,
            "reject-all" => TrackedChangesMode.RejectAll,
            _ => default,
        };

        return value is "fail" or "accept-all" or "reject-all";
    }

    private static bool TryParseAltChunkMode(string? value, out AltChunkMode mode)
    {
        mode = value switch
        {
            "reject" => AltChunkMode.Reject,
            "resolve" => AltChunkMode.Resolve,
            _ => default,
        };

        return value is "reject" or "resolve";
    }

    private static bool TryParseThemePolicy(string? value, out ThemePolicy mode)
    {
        mode = value switch
        {
            "base-wins" => ThemePolicy.BaseWins,
            "import-first" => ThemePolicy.ImportFirst,
            "template-wins" => ThemePolicy.TemplateWins,
            _ => default,
        };

        return value is "base-wins" or "import-first" or "template-wins";
    }

    private static bool TryParseExternalResourceMode(string? value, out ExternalResourceMode mode)
    {
        mode = value switch
        {
            "preserve-links" => ExternalResourceMode.PreserveLinks,
            "materialize" => ExternalResourceMode.Materialize,
            _ => default,
        };

        return value is "preserve-links" or "materialize";
    }

    private static bool TryParseLogFormat(string? value, out LogFormat format)
    {
        format = value switch
        {
            "text" => LogFormat.Text,
            "json" => LogFormat.Json,
            _ => default,
        };

        return value is "text" or "json";
    }
}
