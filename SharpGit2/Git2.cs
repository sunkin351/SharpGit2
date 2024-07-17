using System.Runtime.InteropServices;

namespace SharpGit2;

internal static unsafe partial class Git2
{
    internal const string LibraryName = "";

    [LibraryImport(Git2.LibraryName)]
    internal static partial void git_commitarray_dispose(git_commitarray* commitarray);

    [LibraryImport(LibraryName)]
    internal static partial void git_strarray_dispose(git_strarray* strarray);

    internal static void ThrowError(GitError code)
    {
        throw new Git2Exception(code);
    }

    internal static void ThrowIfError(GitError code)
    {
        if (code != GitError.OK)
            ThrowError(code);
    }

    internal static bool ErrorOrBoolean(int code)
    {
        if ((uint)code > 1u)
            ThrowError((GitError)code);

        return code == 1;
    }

    internal struct git_commitarray
    {
        public void* commits;
        public nuint count;
    }

    internal struct git_strarray
    {
        public byte** strings;
        public nuint count;
    }
}
