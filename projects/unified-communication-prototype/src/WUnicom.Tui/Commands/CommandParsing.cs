using WUnicom.Tui.State;

namespace WUnicom.Tui.Commands;

public static class CommandParsing
{
    public static ParsedCommand? Parse(
        AppState state,
        string input,
        IReadOnlyList<CommandDefinition> commands)
    {
        var trimmed = input.Trim();
        if (!trimmed.StartsWith(':'))
        {
            return null;
        }

        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var definition = CommandRegistry.FindCommand(commands, parts[0]);
        var command = definition?.CanonicalName ?? parts[0];

        state.CommandText = string.Empty;
        state.CommandCursorIndex = 0;
        state.CommandError = null;
        return new ParsedCommand(command, parts.Skip(1).ToArray(), trimmed, definition);
    }
}
