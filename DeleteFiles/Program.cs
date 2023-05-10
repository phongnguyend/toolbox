namespace DeleteFiles;

internal class Program
{
    static void Main(string[] args)
    {
        var ignoredFolders = new List<string>();
        var ignoredFiles = new[]
        {
            "README.txt"
        };

        var oneMonthAgo = DateTime.Now.AddMonths(-1);


        var folders = new[]
        {
            @"D:\XXX\SSS"
        };

        foreach (var folder in folders)
        {
            var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (ignoredFiles.Any(x => file.EndsWith(x)))
                {
                    continue;
                }

                var fileInfor = new FileInfo(file);
                var lastUpdatedTime = Max(fileInfor.CreationTime, fileInfor.LastWriteTime);

                if (lastUpdatedTime > oneMonthAgo)
                {
                    continue;
                }

                fileInfor.Delete();
            }
        }
    }

    private static DateTime Max(DateTime date1, DateTime date2)
    {
        return date1 > date2 ? date1 : date2;
    }
}