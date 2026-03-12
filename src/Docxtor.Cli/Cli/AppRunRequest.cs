namespace Docxtor.Cli.Cli;

internal sealed record AppRunRequest
{
    public string[] Inputs { get; init; } = [];

    public string? OutputPath { get; init; }

    public string? ReportPath { get; init; }

    public string? TemplatePath { get; init; }

    public bool InsertSourceFileTitles { get; init; }
}
