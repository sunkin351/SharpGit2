using static SharpGit2.NativeApi;

namespace SharpGit2;

public readonly unsafe struct GitBlame(Git2.Blame* nativeHandle) : IDisposable
{
    public readonly Git2.Blame* NativeHandle = nativeHandle;

    public uint HunkCount => git_blame_get_hunk_count(this.NativeHandle);

    public void Dispose()
    {
        git_blame_free(NativeHandle);
    }

    public GitBlame UpdateFromBuffer(ReadOnlySpan<byte> fileData)
    {
        Git2.Blame* result;
        fixed (byte* data = fileData)
        {
            Git2.ThrowIfError(git_blame_buffer(&result, this.NativeHandle, data, (nuint)fileData.Length));
        }

        return new(result);
    }

    public Native.GitBlameHunk* GetHunk(uint index)
    {
        return git_blame_get_hunk_byindex(this.NativeHandle, index);
    }

    public Native.GitBlameHunk* GetHunkByLine(nuint index)
    {
        return git_blame_get_hunk_byline(this.NativeHandle, index);
    }
}
