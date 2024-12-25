using static SharpGit2.GitNativeApi;

namespace SharpGit2;

/// <summary>
/// Representation of a status collection
/// </summary>
/// <param name="nativeHandle">The native object pointer</param>
public unsafe readonly struct GitStatusList(Git2.StatusList* nativeHandle) : IGitHandle
{
    public Git2.StatusList* NativeHandle { get; } = nativeHandle;

    /// <inheritdoc/>
    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_status_list_free(this.NativeHandle);
    }

    /// <summary>
    /// The number of status entries
    /// </summary>
    public nuint Count
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_status_list_entrycount(handle.NativeHandle);
        }
    }

    public GitStatusEntry this[nuint idx]
    {
        get
        {
            var handle = this.ThrowIfNull();

            var ptr = git_status_byindex(handle.NativeHandle, idx);

            if (ptr is null)
                ThrowOutOfRange();

            return new(in *ptr);
        }
    }

    private static void ThrowOutOfRange()
    {
        throw new IndexOutOfRangeException();
    }
}
