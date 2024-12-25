using static SharpGit2.GitNativeApi;

namespace SharpGit2;

/// <summary>
/// Opaque structure representing a submodule
/// </summary>
/// <param name="nativeHandle"></param>
public unsafe readonly struct GitSubmodule(Git2.Submodule* nativeHandle) : IGitHandle
{
    public Git2.Submodule* NativeHandle { get; } = nativeHandle;

    public bool IsNull => NativeHandle == null;

    public void Dispose()
    {
        git_submodule_free(this.NativeHandle);
    }
}
