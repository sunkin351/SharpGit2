using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SharpGit2;

public static unsafe partial class NativeApi
{
    private static readonly nint _libraryHandle;
    private static readonly NativeLibraryLifetimeObject _lifetime;

    static NativeApi()
    {
        string runtimesDirectory = Path.Join(AppContext.BaseDirectory, "runtimes");

        if (Directory.Exists(runtimesDirectory))
        {
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
                bool found = false;

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
                            && NativeLibrary.TryLoad(file, out _libraryHandle))
                        {
                            if (NativeLibrary.TryGetExport(_libraryHandle, "git_libgit2_init", out _))
                            {
                                found = true;
                                break;
                            }

                            NativeLibrary.Free(_libraryHandle);
                            _libraryHandle = 0;
                        }
                    }

                    if (found)
                        break;
                }
            }
        }

        NativeLibrary.SetDllImportResolver(typeof(NativeApi).Assembly, (libraryName, assembly, searchPath) =>
        {
            if (libraryName == Git2.LibraryName && _libraryHandle != 0)
            {
                return _libraryHandle;
            }

            // Fallback to the default search behavior
            if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out nint handle))
            {
                return handle;
            }

            return 0;
        });

        // Init the native library
        var code = git_libgit2_init();
        if (code < 0)
            Git2.ThrowError((GitError)code);

        _lifetime = new();

        git_credential_userpass = (delegate* unmanaged[Cdecl]<Git2.Credential**, byte*, byte*, GitCredentialType, nint, int>)NativeLibrary.GetExport(_libraryHandle, nameof(git_credential_userpass));
    }

    [GeneratedRegex("^(lib)?git2-[0-9a-f]+\\.(dll|so|dylib)$")]
    private static partial Regex FileNameRegex();

    private sealed class NativeLibraryLifetimeObject
    {
        ~NativeLibraryLifetimeObject()
        {
            _ = git_libgit2_shutdown();
        }
    }
}
