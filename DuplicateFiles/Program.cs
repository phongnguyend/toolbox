
var directory = args != null && args.Length > 0 ? args[0] : @"../test";
var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

foreach (var file in files)
{
    var fileInfor = new FileInfo(file);
    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
    if (fileNameWithoutExtension.EndsWith("_a"))
    {
        continue;
    }

    var copied = Path.Combine(fileInfor.DirectoryName!, Path.GetFileNameWithoutExtension(file) + "_a" + fileInfor.Extension);

    if (File.Exists(copied))
    {
        continue;
    }

    File.Copy(file, copied);
}