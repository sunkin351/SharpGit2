using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitRemote(Git2.Remote* handle) : IDisposable
{
    public readonly Git2.Remote* NativeHandle = handle;

    public void Dispose()
    {
        git_remote_free(this.NativeHandle);
    }
}
