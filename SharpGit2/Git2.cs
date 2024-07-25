using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2;

public static unsafe partial class Git2
{
    internal const string LibraryName = "";

    public static int Initialize()
    {
        return NativeApi.git_libgit2_init();
    }

    public static int Shutdown()
    {
        return NativeApi.git_libgit2_shutdown();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Exception ExceptionForError(GitError error, string? message = null)
    {
        if (message is null)
        {
            var err = NativeApi.git_error_last();

            Debug.Assert(err != null);
            Debug.Assert(err->@class == (int)error);

            message = Utf8StringMarshaller.ConvertToManaged(err->Message)!;
        }

        return new Git2Exception(error, message);
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

    internal struct Error
    {
        public byte* Message;
        public int @class;
    }
#pragma warning restore IDE1006
}
