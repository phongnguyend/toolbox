using CryptographyHelper.HashAlgorithms;
using System.IO;

namespace FoldersComparer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var path1 = @"D:\folder1";
            var path2 = @"D:\folder2";

            if (File.Exists("results.txt"))
                File.Delete("results.txt");

            var files = Directory.EnumerateFiles(path1, "*", SearchOption.AllDirectories);
            foreach (var file1 in files)
            {
                if (file1.Contains("\\.svn\\"))
                    continue;
                if (file1.Contains("\\.git\\"))
                    continue;
                if (file1.Contains("\\.vs\\"))
                    continue;
                if (file1.Contains("\\node_modules\\"))
                    continue;
                if (file1.Contains("\\bin\\"))
                    continue;
                if (file1.Contains("\\obj\\"))
                    continue;

                var file2 = path2 + file1.Substring(path1.Length);

                var hash1 = GetHash(file1);
                var hash2 = GetHash(file2);

                if (hash1 != hash2)
                {
                    Console.WriteLine($"");
                    Console.WriteLine($"{file1}: {hash1}");
                    Console.WriteLine($"{file2}: {hash2}");
                    Console.WriteLine($"");

                    File.AppendAllLines("results.txt", new[] { $"{file1}: {hash1}", $"{file2}: {hash2}", string.Empty });
                }
                Console.Write(".");
            }

            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        private static string GetHash(string file)
        {
            var fileInfo = new FileInfo(file);
            if (!fileInfo.Exists)
                return null;
            return fileInfo.UseMd5().ComputeHashedString();
        }
    }
}