using TempFolderManager;

class Program
{
    static void Main(string[] args)
    {
        //args = "analyze --greater-than-or-equal \"2024-01-01\" --less-than \"2024-02-01\"".Split(' ');
        //args = "analyze --less-than-or-equal \"2026-02-01\"".Split(' ');
        //args = "delete --less-than \"2026-03-01\"".Split(' ');
        //args = "analyze".Split(' ');

        var parsedArgs = CommandLineParser.ParseParameters(args);

        if (parsedArgs.Errors.Any())
        {
            Console.WriteLine("Error parsing arguments:");
            foreach (var error in parsedArgs.Errors)
            {
                Console.WriteLine($"  - {error}");
            }

            Console.WriteLine();
            Console.WriteLine("Usage: AnalyzeTempFolder <command> [options]");
            Console.WriteLine("Commands:");
            Console.WriteLine("  analyze - Analyze temp folder statistics");
            Console.WriteLine("  delete - Delete files matching criteria");
            Console.WriteLine("Options:");
            Console.WriteLine("  --less-than <date>");
            Console.WriteLine("  --greater-than <date>");
            Console.WriteLine("  --less-than-or-equal <date>");
            Console.WriteLine("  --greater-than-or-equal <date>");

            return;
        }

        string tempPath = Path.GetTempPath();

        switch (parsedArgs.Command)
        {
            case "analyze":
                HandleAnalyzeCommand(tempPath, parsedArgs);
                break;
            case "delete":
                HandleDeleteCommand(tempPath, parsedArgs);
                break;
            default:
                Console.WriteLine("Usage: AnalyzeTempFolder <command> [options]");
                Console.WriteLine("Commands:");
                Console.WriteLine("  analyze - Analyze temp folder statistics");
                Console.WriteLine("  delete - Delete files matching criteria");
                Console.WriteLine("Options:");
                Console.WriteLine("  --less-than <date>");
                Console.WriteLine("  --greater-than <date>");
                Console.WriteLine("  --less-than-or-equal <date>");
                Console.WriteLine("  --greater-than-or-equal <date>");
                break;
        }
    }

    static void HandleAnalyzeCommand(string tempPath, ParsedArguments args)
    {
        Console.WriteLine($"Analyzing files from temp folder: {tempPath}");
        Console.WriteLine();

        var files = Directory
            .EnumerateFiles(tempPath, "*", SearchOption.AllDirectories)
            .Select(file => new FileInfo(file))
            .Where(f => MatchesDateFilter(f.LastWriteTime.Date, args));

        var stats = files
            .GroupBy(f => f.LastWriteTime.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count(),
                Size = g.Sum(f => f.Length)
            })
            .ToList();

        if (stats.Count == 0)
        {
            Console.WriteLine("No files found.");
            return;
        }

        // Print table header
        Console.WriteLine($"{"Date",-12} | {"Files",10} | {"Size",15}");
        Console.WriteLine(new string('-', 12) + "-+-" + new string('-', 10) + "-+-" + new string('-', 15));

        // Print data rows
        long totalFiles = 0;
        long totalSize = 0;
        foreach (var item in stats)
        {
            var formattedDate = item.Date.ToString("yyyy-MM-dd");
            var formattedSize = FormatSize(item.Size);
            Console.WriteLine($"{formattedDate,-12} | {item.Count,10} | {formattedSize,15}");
            totalFiles += item.Count;
            totalSize += item.Size;
        }

        // Print total line
        Console.WriteLine(new string('-', 12) + "-+-" + new string('-', 10) + "-+-" + new string('-', 15));
        var totalFormattedSize = FormatSize(totalSize);
        Console.WriteLine($"{"TOTAL",-12} | {totalFiles,10} | {totalFormattedSize,15}");
    }

    static void HandleDeleteCommand(string tempPath, ParsedArguments args)
    {
        Console.WriteLine($"Analyzing files from temp folder: {tempPath}");
        Console.WriteLine();

        var files = Directory
            .EnumerateFiles(tempPath, "*", SearchOption.AllDirectories)
            .Select(file => new FileInfo(file))
            .Where(f => MatchesDateFilter(f.LastWriteTime.Date, args))
            .ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("No files found matching criteria.");
            return;
        }

        Console.WriteLine($"Found {files.Count} files to delete:");

        var stats = files
            .GroupBy(f => f.LastWriteTime.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count(),
                Size = g.Sum(f => f.Length)
            })
            .ToList();

        // Print table header
        Console.WriteLine($"{"Date",-12} | {"Files",10} | {"Size",15}");
        Console.WriteLine(new string('-', 12) + "-+-" + new string('-', 10) + "-+-" + new string('-', 15));

        // Print data rows
        long totalFiles = 0;
        long totalSize = 0;
        foreach (var item in stats)
        {
            var formattedDate = item.Date.ToString("yyyy-MM-dd");
            var formattedSize = FormatSize(item.Size);
            Console.WriteLine($"{formattedDate,-12} | {item.Count,10} | {formattedSize,15}");
            totalFiles += item.Count;
            totalSize += item.Size;
        }

        // Print total line
        Console.WriteLine(new string('-', 12) + "-+-" + new string('-', 10) + "-+-" + new string('-', 15));
        var totalFormattedSize = FormatSize(totalSize);
        Console.WriteLine($"{"TOTAL",-12} | {totalFiles,10} | {totalFormattedSize,15}");

        Console.WriteLine();
        Console.WriteLine($"Total size: {FormatSize(totalSize)}");
        Console.Write("Are you sure you want to delete these files? (y/n): ");

        if (Console.ReadLine()?.ToLower() == "y")
        {
            int deletedCount = 0;
            foreach (var file in files)
            {
                try
                {
                    file.Delete();
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete {file.FullName}: {ex.Message}");
                }
            }
            Console.WriteLine($"Deleted {deletedCount} files.");
        }
        else
        {
            Console.WriteLine("Deletion cancelled.");
        }
    }

    static bool MatchesDateFilter(DateTime date, ParsedArguments args)
    {
        if (args.LessThan.HasValue && date >= args.LessThan.Value)
            return false;

        if (args.LessThanOrEqual.HasValue && date > args.LessThanOrEqual.Value)
            return false;

        if (args.GreaterThan.HasValue && date <= args.GreaterThan.Value)
            return false;

        if (args.GreaterThanOrEqual.HasValue && date < args.GreaterThanOrEqual.Value)
            return false;

        return true;
    }

    static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}