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
        var normalizedInputs = inputs
            .Select(input => (Input: input, NormalizedPath: Normalize(input.PathOrId)))
            .ToArray();
        var normalizedOutputPath = Normalize(outputPath);
        var normalizedReportPath = Normalize(reportPath);

        if (PathComparer.Equals(normalizedOutputPath, normalizedReportPath))
        {
            return "Output path and report path must be different files.";
        }

        foreach (var (input, normalizedInputPath) in normalizedInputs)
        {
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

        if (normalizedInputs.Any(item => PathComparer.Equals(item.NormalizedPath, normalizedTemplatePath)))
        {
            return "Template path must be different from every input DOCX.";
        }

        return null;
    }

    private static string Normalize(string path)
        => ResolvePathAlias(Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)));

    private static string ResolvePathAlias(string fullPath)
    {
        if (File.Exists(fullPath))
        {
            return ResolveExistingPathAlias(new FileInfo(fullPath));
        }

        if (Directory.Exists(fullPath))
        {
            return ResolveExistingPathAlias(new DirectoryInfo(fullPath));
        }

        var parentDirectory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            return fullPath;
        }

        return Path.Combine(
            ResolvePathAlias(Path.TrimEndingDirectorySeparator(parentDirectory)),
            Path.GetFileName(fullPath));
    }

    private static string ResolveExistingPathAlias(FileSystemInfo pathInfo)
    {
        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(pathInfo.FullName));

        try
        {
            var resolvedPath = pathInfo.ResolveLinkTarget(returnFinalTarget: true)?.FullName;
            if (!string.IsNullOrWhiteSpace(resolvedPath))
            {
                return ResolvePathAlias(Path.TrimEndingDirectorySeparator(Path.GetFullPath(resolvedPath)));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        var parentDirectory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            return fullPath;
        }

        return Path.Combine(
            ResolvePathAlias(Path.TrimEndingDirectorySeparator(parentDirectory)),
            Path.GetFileName(fullPath));
    }
}
