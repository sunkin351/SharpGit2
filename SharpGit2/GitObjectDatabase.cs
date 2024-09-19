using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly partial struct GitObjectDatabase : IDisposable
{
    internal readonly Git2.ObjectDatabase* NativeHandle;

    internal GitObjectDatabase(Git2.ObjectDatabase* nativeHandle)
    {
        NativeHandle = nativeHandle;
    }

    public void Dispose()
    {
        git_odb_free(NativeHandle);
    }
}
