namespace TempFolderManager;

public record ParsedArguments(string Command, DateTime? LessThan, DateTime? GreaterThan, DateTime? LessThanOrEqual, DateTime? GreaterThanOrEqual, List<string> Errors);

public static class CommandLineParser
{
    private static readonly HashSet<string> ValidOptions = new()
    {
        "--less-than",
        "--greater-than",
        "--less-than-or-equal",
        "--greater-than-or-equal"
    };

    private static readonly HashSet<string> ValidCommands = new()
    {
        "analyze",
        "delete"
    };

    public static ParsedArguments ParseParameters(string[] args)
    {
        string command = string.Empty;
        DateTime? lessThan = null;
        DateTime? greaterThan = null;
        DateTime? lessThanOrEqual = null;
        DateTime? greaterThanOrEqual = null;
        var errors = new List<string>();

        int i = 0;

        // Extract command (first argument, must not be an option)
        if (i < args.Length && !args[i].StartsWith("--"))
        {
            command = args[i];
            
            if (!ValidCommands.Contains(command))
            {
                errors.Add($"Unknown command: '{command}'. Valid commands are: {string.Join(", ", ValidCommands)}");
            }
            i++;
        }

        // Parse options
        while (i < args.Length)
        {
            if (args[i] == "--less-than" && i + 1 < args.Length)
            {
                if (DateTime.TryParse(RemoveQuotes(args[i + 1]), out DateTime value))
                {
                    lessThan = value;
                }
                else
                {
                    errors.Add($"Invalid datetime value for --less-than: '{args[i + 1]}'");
                }
                i += 2;
            }
            else if (args[i] == "--greater-than" && i + 1 < args.Length)
            {
                if (DateTime.TryParse(RemoveQuotes(args[i + 1]), out DateTime value))
                {
                    greaterThan = value;
                }
                else
                {
                    errors.Add($"Invalid datetime value for --greater-than: '{args[i + 1]}'");
                }
                i += 2;
            }
            else if (args[i] == "--less-than-or-equal" && i + 1 < args.Length)
            {
                if (DateTime.TryParse(RemoveQuotes(args[i + 1]), out DateTime value))
                {
                    lessThanOrEqual = value;
                }
                else
                {
                    errors.Add($"Invalid datetime value for --less-than-or-equal: '{args[i + 1]}'");
                }
                i += 2;
            }
            else if (args[i] == "--greater-than-or-equal" && i + 1 < args.Length)
            {
                if (DateTime.TryParse(RemoveQuotes(args[i + 1]), out DateTime value))
                {
                    greaterThanOrEqual = value;
                }
                else
                {
                    errors.Add($"Invalid datetime value for --greater-than-or-equal: '{args[i + 1]}'");
                }
                i += 2;
            }
            else if (ValidOptions.Contains(args[i]) && i + 1 >= args.Length)
            {
                errors.Add($"Option '{args[i]}' requires a value");
                i++;
            }
            else if (args[i].StartsWith("--"))
            {
                errors.Add($"Unknown option: '{args[i]}'. Valid options are: {string.Join(", ", ValidOptions)}");
                i++;
            }
            else
            {
                i++;
            }
        }

        return new ParsedArguments(command, lessThan, greaterThan, lessThanOrEqual, greaterThanOrEqual, errors);
    }

    private static string RemoveQuotes(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
            (value.StartsWith("'") && value.EndsWith("'")))
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }
}
