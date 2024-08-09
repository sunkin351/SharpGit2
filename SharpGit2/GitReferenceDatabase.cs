namespace SharpGit2;

public unsafe readonly struct GitReferenceDatabase : IDisposable
{
    internal readonly Git2.ReferenceDatabase* NativeHandle;

    internal GitReferenceDatabase(Git2.ReferenceDatabase* handle)
    {
        NativeHandle = handle;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
