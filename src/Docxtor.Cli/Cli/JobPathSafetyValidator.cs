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
        => Path.TrimEndingDirectorySeparator(ResolvePathAlias(Path.GetFullPath(path)));

    private static string ResolvePathAlias(string fullPath)
    {
        if (File.Exists(fullPath))
        {
            return ResolveExistingPath(new FileInfo(fullPath));
        }

        if (Directory.Exists(fullPath))
        {
            return ResolveExistingPath(new DirectoryInfo(fullPath));
        }

        var parentDirectory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            return fullPath;
        }

        return Path.Combine(
            ResolveDirectoryAlias(parentDirectory),
            Path.GetFileName(fullPath));
    }

    private static string ResolveDirectoryAlias(string directoryPath)
    {
        var fullDirectoryPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directoryPath));
        if (Directory.Exists(fullDirectoryPath))
        {
            return ResolveExistingPath(new DirectoryInfo(fullDirectoryPath));
        }

        var parentDirectory = Path.GetDirectoryName(fullDirectoryPath);
        if (string.IsNullOrWhiteSpace(parentDirectory))
        {
            return fullDirectoryPath;
        }

        return Path.Combine(
            ResolveDirectoryAlias(parentDirectory),
            Path.GetFileName(fullDirectoryPath));
    }

    private static string ResolveExistingPath(FileSystemInfo pathInfo)
    {
        try
        {
            var resolvedPath = pathInfo.ResolveLinkTarget(returnFinalTarget: true)?.FullName;
            return string.IsNullOrWhiteSpace(resolvedPath)
                ? Path.GetFullPath(pathInfo.FullName)
                : Path.GetFullPath(resolvedPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Path.GetFullPath(pathInfo.FullName);
        }
    }
}
