using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitTransport(Git2.Transport* handle) : IGitHandle
{
    public readonly Git2.Transport* NativeHandle = handle;

    public bool IsNull => this.NativeHandle == null;

    void IDisposable.Dispose()
    {
    }
}