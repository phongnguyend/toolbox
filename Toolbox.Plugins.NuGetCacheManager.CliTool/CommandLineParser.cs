namespace NuGetCacheManager;

public record ParsedArguments(string Command, string OrderBy, string OrderByDescending, List<string> Errors);

public static class CommandLineParser
{
    private static readonly HashSet<string> ValidCommands = ["analyze"];

    private static readonly HashSet<string> ValidOrderByValues = ["name", "versions", "size"];

    public static ParsedArguments ParseParameters(string[] args)
    {
        string command = string.Empty;
        string orderBy = "name";
        string orderByDescending = string.Empty;
        var errors = new List<string>();

        int i = 0;

        if (i < args.Length && !args[i].StartsWith("--"))
        {
            command = args[i];

            if (!ValidCommands.Contains(command))
            {
                errors.Add($"Unknown command: '{command}'. Valid commands are: {string.Join(", ", ValidCommands)}");
            }

            i++;
        }

        while (i < args.Length)
        {
            if (args[i] == "--order-by" || args[i] == "--order-by-descending")
            {
                bool descending = args[i] == "--order-by-descending";

                if (i + 1 >= args.Length)
                {
                    errors.Add($"Option '{args[i]}' requires a value ({string.Join(", ", ValidOrderByValues)})");
                    i++;
                }
                else
                {
                    string value = args[i + 1].ToLower();
                    if (!ValidOrderByValues.Contains(value))
                    {
                        errors.Add($"Invalid '{args[i]}' value '{args[i + 1]}'. Valid values are: {string.Join(", ", ValidOrderByValues)}");
                    }
                    else if (descending)
                    {
                        orderByDescending = value;
                    }
                    else
                    {
                        orderBy = value;
                    }
                    i += 2;
                }
            }
            else if (args[i].StartsWith("--"))
            {
                errors.Add($"Unknown option: '{args[i]}'");
                i++;
            }
            else
            {
                i++;
            }
        }

        return new ParsedArguments(command, orderBy, orderByDescending, errors);
    }
}
