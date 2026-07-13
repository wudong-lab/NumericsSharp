using System.Reflection;
using System.Runtime.InteropServices;
using NumericsSharp.Mkl.Native;

namespace NumericsSharp.Mkl.Tests.Native;

internal static class NativeLibraryTestResolver
{
    private static int _isRegistered;

    public static bool TryRegister()
    {
        var nativeLibraryPath = TryFindNativeLibraryPath();
        if (nativeLibraryPath is null)
        {
            return false;
        }

        if (Interlocked.Exchange(ref _isRegistered, 1) == 0)
        {
            NativeLibrary.SetDllImportResolver(
                typeof(InteropInfo).Assembly,
                (_, assembly, searchPath) => Resolve(nativeLibraryPath, assembly, searchPath));
        }

        return true;
    }

    private static IntPtr Resolve(string nativeLibraryPath, Assembly assembly, DllImportSearchPath? searchPath)
    {
        return NativeLibrary.Load(nativeLibraryPath, assembly, searchPath);
    }

    private static string? TryFindNativeLibraryPath()
    {
        var outputCandidate = Path.Combine(AppContext.BaseDirectory, $"{InteropInfo.LibraryName}.dll");
        if (File.Exists(outputCandidate))
        {
            return outputCandidate;
        }

        var repositoryRoot = FindRepositoryRoot();
        if (repositoryRoot is null)
        {
            return null;
        }

        var candidate = Path.Combine(
            repositoryRoot,
            "src",
            "NumericsSharp.Mkl.Native",
            "build",
            "win-x64",
            "Debug",
            $"{InteropInfo.LibraryName}.dll");

        return File.Exists(candidate) ? candidate : null;
    }

    private static string? FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}