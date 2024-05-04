# QuickDiff
A dotnet CLI tool for comparing files and directories.

### Prereqs

You will need .NET 8 Runtime/SDK in order to code / build / use as a dotnet tool.

- [Install .NET 8 SDK/Runtime Interactively](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

OR use CommandLine

- `winget install dotnet-runtime-8`
- `winget install dotnet-sdk-8`

### Install latest stable: 

`dotnet tool install -g QuickDiff --interactive`

### Using the tool
From a [Terminal](https://apps.microsoft.com/detail/9n0dx20hk701) window:
```
Syntax: QuickDiff.exe "<Base File/Dir>" "<Compare File/Dir>"

File Diff Example:
QuickDiff "C:\Users\foo\Desktop\file1.txt" "C:\Users\foo\Desktop\file2.txt"

Directory Diff Example:
QuickDiff "C:\Users\foo\Desktop\Dir1" "C:\Users\foo\Desktop\Dir2"

Save output to a file:
QuickDiff "C:\Users\foo\Desktop\file1.txt" "C:\Users\foo\Desktop\file2.txt" >outputFile.txt
```