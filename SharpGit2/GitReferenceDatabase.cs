using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitReferenceDatabase(Git2.ReferenceDatabase* handle) : IDisposable
{
    internal readonly Git2.ReferenceDatabase* NativeHandle = handle;

    public void Dispose()
    {
        git_refdb_free(this.NativeHandle);
    }
}
