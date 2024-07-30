using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SharpGit2;

internal unsafe partial class NativeApi
{
    static NativeApi()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeApi).Assembly, (libraryName, assembly, searchPath) =>
        {
            nint handle;

            if (libraryName == Git2.LibraryName)
            {
                var runtimesDirectory = Path.Join(AppContext.BaseDirectory, "runtimes");

                if (!Directory.Exists(runtimesDirectory))
                {
                    goto Skip;
                }

                string arch = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

                List<string> possibleDirectories = [];

                if (OperatingSystem.IsWindows())
                {
                    var path = Path.Combine(runtimesDirectory, $"win-{arch}", "native");
                    if (Directory.Exists(path))
                        possibleDirectories.Add(path);
                }
                else if (OperatingSystem.IsMacOSVersionAtLeast(10))
                {
                    var path = Path.Combine(runtimesDirectory, $"osx-{arch}", "native");
                    if (Directory.Exists(path))
                        possibleDirectories.Add(path);
                }
                else if (OperatingSystem.IsLinux())
                {
                    arch = "-" + arch;

                    var enumerable = new FileSystemEnumerable<string>(runtimesDirectory, (ref FileSystemEntry entry) =>
                    {
                        return Path.Join(entry.Directory, entry.FileName, "native");
                    })
                    {
                        ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                        {
                            if (!entry.IsDirectory)
                                return false;

                            var name = entry.FileName;

                            return name.StartsWith("linux-") && name.EndsWith(arch);
                        }
                    };

                    possibleDirectories.AddRange(enumerable);
                }

                if (possibleDirectories.Count > 0)
                {
                    foreach (var dir in possibleDirectories)
                    {
                        foreach (var file in Directory.EnumerateFiles(dir))
                        {
                            var filename = Path.GetFileName(file.AsSpan());

                            // We are relying on LibGit2Sharp's native binary package,
                            // which has an odd naming scheme for it's native library
                            // files. (e.g. the partial git commit hash at the end
                            // just before the extension) Because we can't know what
                            // the file name will be ahead of time, we use a regex to
                            // match any possible libgit2 native library file name.
                            if (FileNameRegex().IsMatch(filename)
                                && NativeLibrary.TryLoad(file, out handle))
                            {
                                return handle;
                            }
                        }
                    }
                }
            }

        Skip:
            // Fallback to the default search behavior
            if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out handle))
            {
                return handle;
            }

            return 0;
        });
    }

    [GeneratedRegex("^(lib)?git2-[0-9a-f]+\\.(dll|so|dylib)$")]
    private static partial Regex FileNameRegex();
}
