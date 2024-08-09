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
        throw new NotImplementedException();
    }
}
