using System.Diagnostics;

if (args.Length == 0)
{
    ShowHelp();
    return;
}

var command = args[0];
var pluginExe = $"phong-{command}";
var pluginArgs = args.Skip(1).ToArray();

var pluginPath = FindPlugin(pluginExe);

if (pluginPath == null)
{
    Console.WriteLine($"Plugin '{command}' not installed.");
    Console.WriteLine($"Install using: dotnet tool install -g phong.{command}");
    return;
}

RunPlugin(pluginPath, pluginArgs);

static void RunPlugin(string exe, string[] args)
{
    var p = new Process();

    p.StartInfo.FileName = exe;
    p.StartInfo.Arguments = string.Join(" ", args);
    p.StartInfo.UseShellExecute = false;

    p.Start();
    p.WaitForExit();
}

static string? FindPlugin(string exe)
{
    var paths = Environment
        .GetEnvironmentVariable("PATH")!
        .Split(Path.PathSeparator);

    foreach (var path in paths)
    {
        var full = Path.Combine(path, exe);

        if (OperatingSystem.IsWindows())
            full += ".exe";

        if (File.Exists(full))
            return full;
    }

    return null;
}

static void ShowHelp()
{
    Console.WriteLine("phong - extensible CLI");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  phong <plugin> [args]");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  phong tfm analyze");
}