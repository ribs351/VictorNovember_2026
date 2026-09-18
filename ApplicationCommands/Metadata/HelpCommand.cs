namespace VictorNovember.ApplicationCommands.Metadata;
public sealed record HelpCommand(
    string Name,
    string Description,
    string Usage,
    string? Example = null);
public sealed record HelpCategory(
    string Name,
    string Description,
    IReadOnlyList<HelpCommand> Commands);
