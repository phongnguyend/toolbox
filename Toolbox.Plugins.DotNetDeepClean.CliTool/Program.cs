// args = ["C:\\Data\\Phong.NguyenDoan\\GitHub\\toolbox"];

if (args.Length == 0)
{
    Console.WriteLine("Usage: phong-dotnet-deep-clean <path>");
    return;
}

string rootPath = args[0];

if (!Directory.Exists(rootPath))
{
    Console.WriteLine($"Directory not found: {rootPath}");
    return;
}

Console.WriteLine($"Searching for bin/obj folders in: {rootPath}");
Console.WriteLine();

var folders = Directory
    .EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories)
    .Where(d =>
    {
        string name = Path.GetFileName(d);
        return name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("obj", StringComparison.OrdinalIgnoreCase);
    })
    .OrderBy(d => d)
    .ToList();

if (folders.Count == 0)
{
    Console.WriteLine("No bin/obj folders found.");
    return;
}

Console.WriteLine($"Found {folders.Count} folder(s):");
Console.WriteLine();

foreach (var folder in folders)
{
    long size = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                         .Sum(f => new FileInfo(f).Length);
    Console.WriteLine($"  {folder} ({FormatSize(size)})");
}

Console.WriteLine();
Console.Write("Are you sure you want to delete these folders? (y/n): ");

if (Console.ReadLine()?.ToLower() == "y")
{
    int deletedCount = 0;
    foreach (var folder in folders)
    {
        if (!Directory.Exists(folder))
            continue;

        try
        {
            Directory.Delete(folder, recursive: true);
            Console.WriteLine($"  Deleted: {folder}");
            deletedCount++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Failed to delete {folder}: {ex.Message}");
        }
    }
    Console.WriteLine();
    Console.WriteLine($"Deleted {deletedCount} folder(s).");
}
else
{
    Console.WriteLine("Deletion cancelled.");
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
