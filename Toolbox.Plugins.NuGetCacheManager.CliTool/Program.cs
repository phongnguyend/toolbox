using NuGetCacheManager;

args = "analyze".Split(' ');
//args = "analyze --order-by versions".Split(' ');
//args = "analyze --order-by-descending versions".Split(' ');
//args = "analyze --order-by size".Split(' ');
//args = "analyze --order-by-descending size".Split(' ');
//args = "analyze --order-by name --order-by-descending size".Split(' ');
//args = "analyze --order-by versions".Split(' ');

var parsedArgs = CommandLineParser.ParseParameters(args);

if (parsedArgs.Errors.Any())
{
    Console.WriteLine("Error parsing arguments:");
    foreach (var error in parsedArgs.Errors)
    {
        Console.WriteLine($"  - {error}");
    }

    Console.WriteLine();
    PrintUsage();
    return;
}

switch (parsedArgs.Command)
{
    case "analyze":
        HandleAnalyzeCommand(parsedArgs.OrderBy, parsedArgs.OrderByDescending);
        break;
    default:
        PrintUsage();
        break;
}

static void PrintUsage()
{
    Console.WriteLine("Usage: phong-ngcm <command> [options]");
    Console.WriteLine("Commands:");
    Console.WriteLine("  analyze - Analyze NuGet cache folder statistics");
    Console.WriteLine("Options:");
    Console.WriteLine("  --order-by <field>             Order results by field ascending: name (default), versions, size");
    Console.WriteLine("  --order-by-descending <field>  Order results by field descending: name, versions, size");
}

static void HandleAnalyzeCommand(string orderBy, string orderByDescending)
{
    string nugetCachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".nuget", "packages");

    if (!Directory.Exists(nugetCachePath))
    {
        Console.WriteLine($"NuGet cache folder not found: {nugetCachePath}");
        return;
    }

    Console.WriteLine($"Analyzing NuGet cache folder: {nugetCachePath}");
    Console.WriteLine();

    var packages = Directory.EnumerateDirectories(nugetCachePath)
        .Select(packageDir =>
        {
            string packageName = Path.GetFileName(packageDir);
            var versions = Directory.EnumerateDirectories(packageDir)
                .Select(versionDir => new
                {
                    Version = Path.GetFileName(versionDir),
                    Size = Directory.EnumerateFiles(versionDir, "*", SearchOption.AllDirectories)
                                    .Sum(f => new FileInfo(f).Length)
                })
                .OrderBy(v => v.Version)
                .ToList();

            return new
            {
                PackageName = packageName,
                VersionCount = versions.Count,
                MinVersion = versions.FirstOrDefault()?.Version ?? string.Empty,
                MaxVersion = versions.LastOrDefault()?.Version ?? string.Empty,
                TotalSize = versions.Sum(v => v.Size)
            };
        });

    var orderedPackages = string.IsNullOrEmpty(orderByDescending)
        ? orderBy switch
        {
            "versions" => packages.OrderBy(p => p.VersionCount).ThenBy(p => p.PackageName),
            "size"     => packages.OrderBy(p => p.TotalSize).ThenBy(p => p.PackageName),
            _          => packages.OrderBy(p => p.PackageName)
        }
        : orderByDescending switch
        {
            "versions" => packages.OrderByDescending(p => p.VersionCount).ThenBy(p => p.PackageName),
            "size"     => packages.OrderByDescending(p => p.TotalSize).ThenBy(p => p.PackageName),
            _          => packages.OrderByDescending(p => p.PackageName)
        };

    var packageList = orderedPackages.ToList();

    if (packageList.Count == 0)
    {
        Console.WriteLine("No packages found.");
        return;
    }

    int nameWidth     = Math.Max("Package Name".Length, packageList.Max(p => p.PackageName.Length));
    int minVerWidth   = Math.Max("Min Version".Length,  packageList.Max(p => p.MinVersion.Length));
    int maxVerWidth   = Math.Max("Max Version".Length,  packageList.Max(p => p.MaxVersion.Length));
    int versionsWidth = "Versions".Length;
    int sizeWidth     = Math.Max("Total Size".Length, 15);

    string header    = $"{"Package Name".PadRight(nameWidth)} | {"Versions".PadLeft(versionsWidth)} | {"Min Version".PadRight(minVerWidth)} | {"Max Version".PadRight(maxVerWidth)} | {"Total Size".PadLeft(sizeWidth)}";
    string separator = new string('-', nameWidth) + "-+-" + new string('-', versionsWidth) + "-+-" + new string('-', minVerWidth) + "-+-" + new string('-', maxVerWidth) + "-+-" + new string('-', sizeWidth);

    Console.WriteLine(header);
    Console.WriteLine(separator);

    long grandTotalSize     = 0;
    int  grandTotalVersions = 0;

    foreach (var package in packageList)
    {
        Console.WriteLine($"{package.PackageName.PadRight(nameWidth)} | {package.VersionCount.ToString().PadLeft(versionsWidth)} | {package.MinVersion.PadRight(minVerWidth)} | {package.MaxVersion.PadRight(maxVerWidth)} | {FormatSize(package.TotalSize).PadLeft(sizeWidth)}");
        grandTotalSize     += package.TotalSize;
        grandTotalVersions += package.VersionCount;
    }

    Console.WriteLine(separator);
    Console.WriteLine($"{"TOTAL".PadRight(nameWidth)} | {grandTotalVersions.ToString().PadLeft(versionsWidth)} | {"".PadRight(minVerWidth)} | {"".PadRight(maxVerWidth)} | {FormatSize(grandTotalSize).PadLeft(sizeWidth)}");
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
