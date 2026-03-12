using Docxtor.Cli.Cli;

Environment.ExitCode = await CliApplication.RunAsync(
    args,
    Console.Out,
    Console.Error,
    Directory.GetCurrentDirectory());
