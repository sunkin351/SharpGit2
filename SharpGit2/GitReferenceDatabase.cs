using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitReferenceDatabase(Git2.ReferenceDatabase* handle) : IGitHandle
{
    public Git2.ReferenceDatabase* NativeHandle { get; } = handle;

    /// <inheritdoc/>
    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_refdb_free(this.NativeHandle);
    }
}
