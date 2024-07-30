namespace SharpGit2;

public unsafe readonly partial struct ObjectDatabaseHandle : IDisposable
{
    internal readonly Git2.ObjectDatabase* NativeHandle;

    internal ObjectDatabaseHandle(Git2.ObjectDatabase* nativeHandle)
    {
        NativeHandle = nativeHandle;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
