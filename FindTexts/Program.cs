using System.Text.RegularExpressions;

namespace FindTexts;

internal class Program
{
    static void Main(string[] args)
    {
        var folders = new[]
        {
            @"D:\XXX\SSS"
        };

        var results = new List<string>();

        foreach (var folder in folders)
        {
            var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(x => x.EndsWith(".config", StringComparison.OrdinalIgnoreCase)
                        || x.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase)
                        || x.EndsWith("index.html", StringComparison.OrdinalIgnoreCase)
                        || x.EndsWith("apiConstant.json", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                string text = File.ReadAllText(file);

                //if (text.Contains("IsEnableHttps"))
                //{
                //    var xx = Regex.Matches(text, "#{(.*?)}");
                //}

                results.AddRange(Regex.Matches(text, "#{(.*?)}").Select(x => x.Groups[1].Value));
            }
        }

        results = results.Distinct().OrderBy(x => x).ToList();
    }
}