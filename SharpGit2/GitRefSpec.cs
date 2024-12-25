using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitRefSpec(Git2.RefSpec* nativeHandle) : IGitHandle
{
    public Git2.RefSpec* NativeHandle { get; } = nativeHandle;

    /// <inheritdoc/>
    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_refspec_free(this.NativeHandle);
    }
}
