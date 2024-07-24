using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2;

internal static unsafe partial class Git2
{
    internal const string LibraryName = "";



    internal static Exception ExceptionForError(GitError error)
    {
        return error switch
        {
            GitError.InvalidSpec => new ArgumentException("Name/Ref Specification was not followed!"),
            _ => new Git2Exception(error),
        };
    }

    internal static void ThrowError(GitError code)
    {
        throw ExceptionForError(code);
    }

    internal static void ThrowIfError(GitError code)
    {
        if (code != GitError.OK)
            ThrowError(code);
    }

    internal static bool ErrorOrBoolean(int code)
    {
        if (code < 0)
            ThrowError((GitError)code);

        return code != 0;
    }

#pragma warning disable IDE1006
    internal struct git_commitarray
    {
        public void* commits;
        public nuint count;
    }

    internal struct git_strarray
    {
        public byte** strings;
        public nuint count;

        public readonly string[] ToManaged()
        {
            if (count > int.MaxValue)
                throw new InvalidOperationException("Too many strings in native string array!");

            var span = new ReadOnlySpan<nint>(strings, (int)count);

            var managedArray = new string[span.Length];

            for (int i = 0; i < span.Length; ++i)
            {
                managedArray[i] = Utf8StringMarshaller.ConvertToManaged((byte*)span[i])!;
            }

            return managedArray;
        }
    }
#pragma warning restore IDE1006
}
