using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChurchProjector.Classes.Ndi;

public static class NdiRuntimeLocator
{
    public static string? FindRuntimeLibrary()
    {
        string[] libraryNames;
        List<string> roots =
        [
            Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR_V6") ?? string.Empty,
            Environment.GetEnvironmentVariable("NDI_RUNTIME_DIR") ?? string.Empty,
            Environment.GetEnvironmentVariable("NDI_LIBRARY_PATH") ?? string.Empty,
            Environment.GetEnvironmentVariable("NDI_SDK_DIR") ?? string.Empty,
        ];

        if (OperatingSystem.IsWindows())
        {
            libraryNames = ["Processing.NDI.Lib.x64.dll"];
            roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NDI"));
            roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NDI"));
        }
        else if (OperatingSystem.IsLinux())
        {
            libraryNames = ["libndi.so", "libndi.so.6", "libndi.so.6.4.0"];
            roots.AddRange(GetLibrarySearchPaths());
            roots.AddRange(
            [
                "/usr/lib",
                "/usr/lib64",
                "/usr/local/lib",
                "/usr/local/lib64",
                "/usr/lib/ndi",
                "/usr/local/lib/ndi",
                "/opt/ndi",
                "/lib",
                "/lib64",
                "/usr/lib/x86_64-linux-gnu",
                "/usr/lib/aarch64-linux-gnu",
                "/lib/x86_64-linux-gnu",
                "/lib/aarch64-linux-gnu",
            ]);
        }
        else if (OperatingSystem.IsMacOS())
        {
            libraryNames = ["libndi.dylib", "libndi.6.dylib"];
            roots.AddRange(GetLibrarySearchPaths());
            roots.Add("/usr/local/lib");
            roots.Add("/opt/homebrew/lib");
            roots.Add("/opt/local/lib");
            roots.Add("/Library/NDI/NDI 6 Runtime/v6");
        }
        else
        {
            return null;
        }

        StringComparer pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        foreach (string root in roots.Where(Directory.Exists).Distinct(pathComparer))
        {
            IEnumerable<string> relativePaths = OperatingSystem.IsWindows()
                ? [string.Empty, "bin", Path.Combine("bin", "x64")]
                :
                [
                    string.Empty,
                    "lib",
                    "lib64",
                    "bin",
                    Path.Combine("lib", "x86_64-linux-gnu"),
                    Path.Combine("lib", "aarch64-linux-gnu"),
                ];
            foreach (string relativePath in relativePaths)
            {
                foreach (string libraryName in libraryNames)
                {
                    string candidate = Path.Combine(root, relativePath, libraryName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        if (OperatingSystem.IsWindows())
        {
            IEnumerable<string> installedRuntimeRoots = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NDI"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "NDI"),
            };
            foreach (string root in installedRuntimeRoots.Where(Directory.Exists).Distinct(pathComparer))
            {
                string? runtimePath = Directory.EnumerateFiles(
                    root,
                    libraryNames[0],
                    SearchOption.AllDirectories).FirstOrDefault();
                if (runtimePath is not null)
                {
                    return runtimePath;
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> GetLibrarySearchPaths()
    {
        string? searchPath = OperatingSystem.IsMacOS()
            ? Environment.GetEnvironmentVariable("DYLD_LIBRARY_PATH")
            : Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
        if (!string.IsNullOrWhiteSpace(searchPath))
        {
            foreach (string path in searchPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                yield return path;
            }
        }
    }
}
