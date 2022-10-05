using System.Globalization;
using System.Xml.Linq;

internal class Program
{
    private static void Main(string[] args)
    {
        var packages = new List<(string Name, string Version, string Project)>();

        var directories = new[]
        {
            @"D:\Project1\UI",
            @"D:\Project2\UI",
        };

        foreach (var directory in directories)
        {
            var packagesInPackagesConfigureFiles = ScanPackagesInPackagesConfigureFiles(directory);
            packages.AddRange(packagesInPackagesConfigureFiles);
        }

        var packageGroups = packages.GroupBy(x => new { x.Name, x.Version })
            .Select(g => new
            {
                g.Key.Name,
                g.Key.Version,
                Projects = string.Join(", ", g.Select(x => x.Project)),
                Url = $"https://www.npmjs.com/package/{g.Key.Name}/v/{FormatVersion(g.Key.Version)}"
            })
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Version).ToList();

        var ignoredPackages = new List<string>
        {
            //"System.",
            //"Microsoft."
        };

        using (var fileStream = File.Open("packages.csv", FileMode.Create))
        {
            using (var streamWriter = new StreamWriter(fileStream))
            {
                foreach (var package in packageGroups)
                {
                    if (ignoredPackages.Any(x => package.Name.StartsWith(x)))
                    {
                        continue;
                    }

                    streamWriter.WriteLine($"{package.Name},{package.Version}, ,\"{package.Url}\",\"{package.Projects}\"");
                }
            }
        }

        //Console.ReadLine();
    }

    private static List<(string Name, string Version, string Project)> ScanPackagesInPackagesConfigureFiles(string directory)
    {
        var files = Directory.GetFiles(directory, "package.json", SearchOption.AllDirectories);
        var packages = new List<(string Name, string Version, string Project)>();
        foreach (var file in files)
        {
            var package = System.Text.Json.JsonSerializer.Deserialize<Package>(File.ReadAllText(file), new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            var projectName = new DirectoryInfo(Path.GetDirectoryName(file)).Name;

            if (package.Dependencies != null)
            {
                foreach (var node in package.Dependencies)
                {
                    var packageName = node.Key;
                    var packageVersion = node.Value;

                    if (packageVersion.StartsWith("file:"))
                    {
                        continue;
                    }

                    packages.Add((packageName, packageVersion, projectName));
                }
            }

            if (package.DevDependencies != null)
            {
                foreach (var node in package.DevDependencies)
                {
                    var packageName = node.Key;
                    var packageVersion = node.Value;

                    if (packageVersion.StartsWith("file:"))
                    {
                        continue;
                    }

                    packages.Add((packageName, packageVersion, projectName));
                }
            }
        }

        return packages;
    }

    private static string FormatVersion(string version)
    {
        if (version.StartsWith("^")
            || version.StartsWith("~"))
        {
            version = version.Substring(1);
        }

        return version;
    }
}

public class Package
{
    public Dictionary<string, string> Dependencies { get; set; }

    public Dictionary<string, string> DevDependencies { get; set; }
}
