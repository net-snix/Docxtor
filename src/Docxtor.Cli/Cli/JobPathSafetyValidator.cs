using Docxtor.Core.Models;

namespace Docxtor.Cli.Cli;

internal static class JobPathSafetyValidator
{
    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    public static string? Validate(
        IReadOnlyList<InputDocument> inputs,
        string outputPath,
        string reportPath,
        string? templatePath)
    {
        var normalizedOutputPath = Normalize(outputPath);
        var normalizedReportPath = Normalize(reportPath);

        if (PathComparer.Equals(normalizedOutputPath, normalizedReportPath))
        {
            return "Output path and report path must be different files.";
        }

        foreach (var input in inputs)
        {
            var normalizedInputPath = Normalize(input.PathOrId);

            if (PathComparer.Equals(normalizedInputPath, normalizedOutputPath))
            {
                return $"Output path '{outputPath}' must be different from input '{input.PathOrId}'.";
            }

            if (PathComparer.Equals(normalizedInputPath, normalizedReportPath))
            {
                return $"Report path '{reportPath}' must be different from input '{input.PathOrId}'.";
            }
        }

        if (string.IsNullOrWhiteSpace(templatePath))
        {
            return null;
        }

        var normalizedTemplatePath = Normalize(templatePath);
        if (PathComparer.Equals(normalizedTemplatePath, normalizedOutputPath))
        {
            return "Output path must be different from the template path.";
        }

        if (PathComparer.Equals(normalizedTemplatePath, normalizedReportPath))
        {
            return "Report path must be different from the template path.";
        }

        if (inputs.Any(input => PathComparer.Equals(Normalize(input.PathOrId), normalizedTemplatePath)))
        {
            return "Template path must be different from every input DOCX.";
        }

        return null;
    }

    private static string Normalize(string path)
        => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}
