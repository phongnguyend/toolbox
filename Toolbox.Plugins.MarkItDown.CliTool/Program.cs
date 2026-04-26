using System.Net.Http.Headers;

HttpClient client = new HttpClient();

foreach (var arg in args)
{
    Console.WriteLine(arg);
    var filePath = arg;

    if (Directory.Exists(filePath))
    {
        foreach (var file in Directory.GetFiles(filePath))
        {
            await ConvertAsync(file);
        }
    }
    else
    {
        await ConvertAsync(filePath);
    }
}

async Task ConvertAsync(string path)
{
    if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"File {path} is already a markdown file, skipping.");
        return;
    }

    Console.WriteLine($"Converting {path}");

    var resultFilePath = path + ".md";

    if (File.Exists(resultFilePath))
    {
        int number = 1;
        while (File.Exists(path + $"({number}).md"))
        {
            number++;
        }
        resultFilePath = path + $"({number}).md";
    }

    using var form = new MultipartFormDataContent();
    using var fileContent = new ByteArrayContent(File.ReadAllBytes(path));
    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
    form.Add(fileContent, "file", Path.GetFileName(path));
    form.Add(new StringContent("Test Name"), "name");

    var response = await client.PostAsync($"http://localhost:8000/convert", form);
    response.EnsureSuccessStatusCode();

    var markdown = await response.Content.ReadAsStringAsync();

    File.WriteAllText(resultFilePath, markdown);

    Console.WriteLine($"Converted {path} -> {resultFilePath}");
}