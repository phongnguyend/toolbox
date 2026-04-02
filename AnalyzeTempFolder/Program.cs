class Program
{
    static void Main()
    {
        string tempPath = Path.GetTempPath();

        Console.WriteLine($"Scanning temp folder: {tempPath}");
        Console.WriteLine();

        var stats = Directory
            .EnumerateFiles(tempPath, "*", SearchOption.AllDirectories)
            .Select(file => new FileInfo(file))
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