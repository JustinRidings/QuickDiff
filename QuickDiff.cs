using System.Reflection;
using System.IO;

namespace QuickDiff
{
    internal class QuickDiff
    {
        static int Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 2)
            {
                PrintHelp();
                //https://learn.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-
                return 87;
            }

            var diffType = TryParseArguments(args);

            switch (diffType)
            {
                case DiffType.File:
                    var ilDiff = GenerateInlineFileDiff(args[0], args[1]); ;
                    foreach(var change in ilDiff)
                    {
                        Console.ForegroundColor = change.Item1;
                        Console.WriteLine(change.Item2);
                    }
                    return 0;
                case DiffType.Directory:
                    var dirDiff = GenerateDirectoryDiff(args[0], args[1]);
                    foreach(var change in dirDiff)
                    {
                        Console.ForegroundColor = change.Item1;
                        Console.WriteLine(change.Item2);
                    }
                    return 0;
                default:
                    PrintHelp();
                    return 3;
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine($"----------------------------");
            Console.WriteLine($"QuickDiff {GetAppVersion()}");
            Console.WriteLine($"----------------------------");
            Console.WriteLine($"\n");
            Console.WriteLine($"Syntax: QuickDiff.exe \"<Base File/Dir>\" \"<Compare File/Dir>\"");
            Console.WriteLine($"\n");
            Console.WriteLine($"File Diff Example:");
            Console.WriteLine($"QuickDiff.exe \"C:\\Users\\foo\\Desktop\\file1.txt\" \"C:\\Users\\foo\\Desktop\\file2.txt\"");
            Console.WriteLine($"\n");
            Console.WriteLine($"Directory Diff Example:");
            Console.WriteLine($"QuickDiff.exe \"C:\\Users\\foo\\Desktop\\Dir1\" \"C:\\Users\\foo\\Desktop\\Dir2\"");
        }

        static string? GetAppVersion()
        {
            // Get the executing assembly (this program)
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Get the assembly version
            Version? version = assembly?.GetName()?.Version;

            return $"v{version?.ToString()}";
        }

        static DiffType TryParseArguments(string[] args)
        {
            if (args != null && args.Length == 2 && !string.IsNullOrEmpty(args[0]) && !string.IsNullOrEmpty(args[1]))
            {
                var item1 = args[0];
                var item2 = args[1];

                try
                {
                    bool item1IsFile = File.Exists(item1) && !IsBinaryFile(Path.GetFullPath(item1));
                    bool item2IsFile = File.Exists(item2) && !IsBinaryFile(Path.GetFullPath(item2));
                    bool item1IsDirectory = Directory.Exists(item1);
                    bool item2IsDirectory = Directory.Exists(item2);

                    if (item1IsFile && item2IsFile)
                    {
                        return DiffType.File;
                    }
                    else if (item1IsDirectory && item2IsDirectory)
                    {
                        return DiffType.Directory;
                    }
                }
                catch
                {

                }
            }

            return DiffType.None;
        }

        public static List<Tuple<ConsoleColor,string>> GenerateInlineFileDiff(string original, string modified)
        {
            List<Tuple<ConsoleColor,string>> result = new List<Tuple<ConsoleColor, string>>();
            string[] originalLines = File.ReadAllLines(original);
            string[] modifiedLines = File.ReadAllLines(modified);

            int maxLength = Math.Max(originalLines.Length, modifiedLines.Length);

            for (int i = 0; i < maxLength; i++)
            {
                string originalLine = i < originalLines.Length ? originalLines[i] : string.Empty;
                string modifiedLine = i < modifiedLines.Length ? modifiedLines[i] : string.Empty;

                if (originalLine == modifiedLine)
                {
                    result.Add(new Tuple<ConsoleColor,string>(ConsoleColor.White, $"{i}: {originalLine}"));
                }
                else
                {
                    if (modifiedLine != null)
                    {
                        result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.Green, $"{i}: ++++    {modifiedLine}"));
                    }
                    if (originalLine != null)
                    {
                        result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.Red, $"{i}: ----    {originalLine}"));
                    }
                }
            }

            return result;
        }

        public static List<Tuple<ConsoleColor, string>> GenerateDirectoryDiff(string baseDir, string compareDir)
        {
            List<Tuple<ConsoleColor,string>> result = new List<Tuple<ConsoleColor, string>>();

            // Get the list of files and directories in baseDir
            string[] baseFiles = Directory.GetFiles(baseDir, "*", SearchOption.AllDirectories);
            string[] baseDirectories = Directory.GetDirectories(baseDir, "*", SearchOption.AllDirectories);
            HashSet<string> baseFileSet = new HashSet<string>(baseFiles);
            HashSet<string> baseDirectorySet = new HashSet<string>(baseDirectories);

            // Get the list of files and directories in compareDir
            string[] compareFiles = Directory.GetFiles(compareDir, "*", SearchOption.AllDirectories);
            string[] compareDirectories = Directory.GetDirectories(compareDir, "*", SearchOption.AllDirectories);
            HashSet<string> compareFileSet = new HashSet<string>(compareFiles);
            HashSet<string> compareDirectorySet = new HashSet<string>(compareDirectories);

            // Compare directories
            foreach (string baseDirectory in baseDirectorySet)
            {
                string relativePath = baseDirectory.Substring(baseDir.Length).TrimStart('\\');
                if (!compareDirectorySet.Contains(Path.Combine(compareDir, relativePath)))
                {
                    // Directory exists in baseDir but not in compareDir
                    result.Add(new Tuple<ConsoleColor,string>(ConsoleColor.Red, $"----    .\\{relativePath}"));
                }
            }

            foreach (string compareDirectory in compareDirectorySet)
            {
                string relativePath = compareDirectory.Substring(compareDir.Length).TrimStart('\\');
                if (!baseDirectorySet.Contains(Path.Combine(baseDir, relativePath)))
                {
                    // Directory exists in compareDir but not in baseDir
                    result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.Green, $"++++    .\\{relativePath}\\"));
                }
            }

            // Compare files
            foreach (string baseFile in baseFileSet)
            {
                string relativePath = baseFile.Substring(baseDir.Length).TrimStart('\\');
                string compareFile = Path.Combine(compareDir, relativePath);

                if (!compareFileSet.Contains(compareFile))
                {
                    // File exists in baseDir but not in compareDir
                    result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.Red, $"----    .\\{relativePath}"));
                }
                else
                {
                    DateTime baseFileDate = File.GetLastWriteTimeUtc(baseFile);
                    DateTime compareFileDate = File.GetLastWriteTimeUtc(compareFile);

                    if (baseFileDate < compareFileDate)
                    {
                        // compareDir version is newer
                        result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.Green, $"ver+    .\\{relativePath}"));
                    }
                    else if (baseFileDate > compareFileDate)
                    {
                        // compareDir version is older
                        result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.Red, $"ver-    .\\{relativePath}"));
                    }
                    else
                    {
                        // File exists in both directories
                        result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.White, $"        .\\{relativePath}"));
                    }
                }
            }

            // Sort the result for better readability
            result = result.OrderBy(r => r).ToList();

            return result;
        }

        static bool IsBinaryFile(string filePath)
        {
            const int bufferLength = 1024;
            byte[] buffer = new byte[bufferLength];
            int controlCharCount = 0;

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int bytesRead = fileStream.Read(buffer, 0, bufferLength);
                for (int i = 0; i < bytesRead; i++)
                {
                    // Check for non-text characters (excluding new lines and tabs)
                    if (buffer[i] > 0 && buffer[i] < 8 || (buffer[i] > 13 && buffer[i] < 32))
                    {
                        controlCharCount++;
                    }
                }
            }

            // If more than 30% of the characters are non-text, it's likely a binary file
            return (double)controlCharCount / bufferLength > 0.3;
        }

        enum DiffType
        {
            File,
            Directory,
            None
        }

    }
}

