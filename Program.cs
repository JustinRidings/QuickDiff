﻿using System.Reflection;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace QuickDiff
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var baseArg = new Argument<string>("base")
            {
                Description = "The base file or directory to compare",
                Arity = ArgumentArity.ExactlyOne
            };
            var compareArg = new Argument<string>("comparison")
            {
                Description = "The base file or directory to compare",
                Arity = ArgumentArity.ExactlyOne
            };
            var versionOption = new Option<bool>("--fileVersion")
            {
                Description = "Whether or not to include file version diffing for directories",
            };
            versionOption.AddAlias("--fv");

            var rootCommand = new RootCommand();
            rootCommand.AddArgument(baseArg);
            rootCommand.AddArgument(compareArg);
            rootCommand.AddOption(versionOption);

            rootCommand.SetHandler((string baseArgValue, string compareArgValue, bool isVersionOption) =>
            {
                if (string.IsNullOrEmpty(baseArgValue) || string.IsNullOrEmpty(compareArgValue))
                {
                    Environment.Exit(87);
                }

                var diffType = GetDiffType(baseArgValue, compareArgValue);

                switch (diffType)
                {
                    case DiffType.File:
                        var ilDiff = GenerateInlineFileDiff(baseArgValue, compareArgValue); ;
                        foreach (var change in ilDiff)
                        {
                            Console.ForegroundColor = change.Item1;
                            Console.WriteLine(change.Item2);
                        }
                        ResetConsoleColor();
                        break;
                    case DiffType.Directory:
                        var dirDiff = GenerateDirectoryDiff(baseArgValue, compareArgValue, isVersionOption);
                        foreach (var change in dirDiff)
                        {
                            Console.ForegroundColor = change.Item1;
                            Console.WriteLine(change.Item2);
                        }
                        ResetConsoleColor();
                        break;
                    default:
                        break;
                }
            },
            baseArg, compareArg, versionOption);

            rootCommand.Invoke(args);

            Environment.Exit(0);
        }

        static void ResetConsoleColor()
        {
            Console.ForegroundColor = ConsoleColor.White;
        }

        static DiffType GetDiffType(string item1, string item2)
        {
            if (!string.IsNullOrEmpty(item1) && !string.IsNullOrEmpty(item2))
            {
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

        static List<Tuple<ConsoleColor, string>> GenerateInlineFileDiff(string original, string modified)
        {
            List<Tuple<ConsoleColor, string>> result = new List<Tuple<ConsoleColor, string>>();
            string[] originalLines = File.ReadAllLines(original);
            string[] modifiedLines = File.ReadAllLines(modified);

            int maxLength = Math.Max(originalLines.Length, modifiedLines.Length);

            for (int i = 0; i < maxLength; i++)
            {
                string originalLine = i < originalLines.Length ? originalLines[i] : string.Empty;
                string modifiedLine = i < modifiedLines.Length ? modifiedLines[i] : string.Empty;

                if (originalLine == modifiedLine)
                {
                    result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.White, $"{i}: {originalLine}"));
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

        static List<Tuple<ConsoleColor, string>> GenerateDirectoryDiff(string baseDir, string compareDir, bool showVersionDiff)
        {
            List<Tuple<ConsoleColor, string>> result = new List<Tuple<ConsoleColor, string>>();

            // Get the list of files and directories in baseDir
            string[] baseFiles = Directory.GetFiles(baseDir, "*.*", SearchOption.AllDirectories);
            string[] baseDirectories = Directory.GetDirectories(baseDir, "*", SearchOption.AllDirectories);
            HashSet<string> baseFileSet = new HashSet<string>(baseFiles.Select(f => f.Substring(baseDir.Length + 1)));
            HashSet<string> baseDirectorySet = new HashSet<string>(baseDirectories.Select(d => d.Substring(baseDir.Length + 1)));

            // Get the list of files and directories in compareDir
            string[] compareFiles = Directory.GetFiles(compareDir, "*.*", SearchOption.AllDirectories);
            string[] compareDirectories = Directory.GetDirectories(compareDir, "*", SearchOption.AllDirectories);
            HashSet<string> compareFileSet = new HashSet<string>(compareFiles.Select(f => f.Substring(compareDir.Length + 1)));
            HashSet<string> compareDirectorySet = new HashSet<string>(compareDirectories.Select(d => d.Substring(compareDir.Length + 1)));

            // Compare directories
            foreach (string baseDirectory in baseDirectorySet)
            {
                if (!compareDirectorySet.Contains(baseDirectory))
                {
                    // Directory exists in baseDir but not in compareDir
                    result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.Red, $"----    .\\{baseDirectory}"));
                }
            }

            foreach (string compareDirectory in compareDirectorySet)
            {
                if (!baseDirectorySet.Contains(compareDirectory))
                {
                    // Directory exists in compareDir but not in baseDir
                    result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.Green, $"++++    .\\{compareDirectory}\\"));
                }
            }

            // Compare files
            foreach (string baseFile in baseFileSet)
            {
                if (!compareFileSet.Contains(baseFile))
                {
                    // File exists in baseDir but not in compareDir
                    result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.Red, $"----    .\\{baseFile}"));
                }
                else
                {
                    string baseFilePath = Path.Combine(baseDir, baseFile);
                    string compareFilePath = Path.Combine(compareDir, baseFile);

                    DateTime baseFileDate = File.GetLastWriteTimeUtc(baseFilePath);
                    DateTime compareFileDate = File.GetLastWriteTimeUtc(compareFilePath);

                    if (baseFileDate < compareFileDate)
                    {
                        if (showVersionDiff)
                        {
                            // compareDir version is newer
                            result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.Green, $"ver+    .\\{baseFile}"));
                        }
                        else
                        {
                            result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.White, $"        .\\{baseFile}"));
                        }
                    }
                    else if (baseFileDate > compareFileDate)
                    {
                        if (showVersionDiff)
                        {
                            // compareDir version is older
                            result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.Red, $"ver-    .\\{baseFile}"));
                        }
                        else
                        {
                            result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.White, $"        .\\{baseFile}"));
                        }
                    }
                    else
                    {
                        // File exists in both directories
                        result.Add(new Tuple<ConsoleColor, string>(ConsoleColor.White, $"        .\\{baseFile}"));
                    }
                }
            }

            // Sort the result for better readability
            result = result.OrderBy(r => r.Item2).ToList();

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

