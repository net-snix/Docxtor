using Docxtor.Core.Models;

namespace Docxtor.Cli.Cli;

internal static class MergeOptionParsers
{
    public static bool TryParseBoundaryMode(string? value, out BoundaryMode mode)
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

    public static bool TryParseNumberingMode(string? value, out NumberingMode mode)
    {
        mode = value switch
        {
            "preserve-source" => NumberingMode.PreserveSource,
            "continue-destination" => NumberingMode.ContinueDestination,
            _ => default,
        };

        return value is "preserve-source" or "continue-destination";
    }

    public static bool TryParseTrackedChangesMode(string? value, out TrackedChangesMode mode)
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

    public static bool TryParseAltChunkMode(string? value, out AltChunkMode mode)
    {
        mode = value switch
        {
            "reject" => AltChunkMode.Reject,
            "resolve" => AltChunkMode.Resolve,
            _ => default,
        };

        return value is "reject" or "resolve";
    }

    public static bool TryParseThemePolicy(string? value, out ThemePolicy mode)
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

    public static bool TryParseExternalResourceMode(string? value, out ExternalResourceMode mode)
    {
        mode = value switch
        {
            "preserve-links" => ExternalResourceMode.PreserveLinks,
            "materialize" => ExternalResourceMode.Materialize,
            _ => default,
        };

        return value is "preserve-links" or "materialize";
    }

    public static bool TryParseLogFormat(string? value, out LogFormat format)
    {
        format = value switch
        {
            "text" => LogFormat.Text,
            "json" => LogFormat.Json,
            _ => default,
        };

        return value is "text" or "json";
    }

    public static BoundaryMode? ParseBoundaryMode(string? value)
        => TryParseBoundaryMode(value, out var mode) ? mode : null;

    public static NumberingMode? ParseNumberingMode(string? value)
        => TryParseNumberingMode(value, out var mode) ? mode : null;

    public static TrackedChangesMode? ParseTrackedChangesMode(string? value)
        => TryParseTrackedChangesMode(value, out var mode) ? mode : null;

    public static AltChunkMode? ParseAltChunkMode(string? value)
        => TryParseAltChunkMode(value, out var mode) ? mode : null;

    public static ThemePolicy? ParseThemePolicy(string? value)
        => TryParseThemePolicy(value, out var mode) ? mode : null;

    public static ExternalResourceMode? ParseExternalResourceMode(string? value)
        => TryParseExternalResourceMode(value, out var mode) ? mode : null;

    public static LogFormat? ParseLogFormat(string? value)
        => TryParseLogFormat(value, out var format) ? format : null;
}
