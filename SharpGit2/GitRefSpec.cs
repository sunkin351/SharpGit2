using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitRefSpec(Git2.RefSpec* nativeHandle) : IDisposable
{
    public readonly Git2.RefSpec* NativeHandle = nativeHandle;

    public void Dispose()
    {
        git_refspec_free(this.NativeHandle);
    }
}
