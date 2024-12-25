using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public readonly unsafe struct GitBlame(Git2.Blame* nativeHandle) : IDisposable, IGitHandle
{
    public Git2.Blame* NativeHandle { get; } = nativeHandle;

    public bool IsNull => this.NativeHandle == null;

    public uint HunkCount
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_blame_get_hunk_count(handle.NativeHandle);
        }
    }

    public void Dispose()
    {
        git_blame_free(NativeHandle);
    }

    public GitBlame UpdateFromBuffer(ReadOnlySpan<byte> fileData)
    {
        var handle = this.ThrowIfNull();

        Git2.Blame* result = null;
        fixed (byte* data = fileData)
        {
            Git2.ThrowIfError(git_blame_buffer(&result, handle.NativeHandle, data, (nuint)fileData.Length));
        }

        return new(result);
    }

    public Native.GitBlameHunk* GetHunk(uint index)
    {
        var handle = this.ThrowIfNull();

        return git_blame_get_hunk_byindex(handle.NativeHandle, index);
    }

    public Native.GitBlameHunk* GetHunkByLine(nuint index)
    {
        var handle = this.ThrowIfNull();

        return git_blame_get_hunk_byline(handle.NativeHandle, index);
    }
}
