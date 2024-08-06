namespace SharpGit2;

public unsafe readonly struct TreeHandle : IDisposable
{
    internal readonly Git2.Tree* NativeHandle;

    internal TreeHandle(Git2.Tree* nativeHandle)
    {
        NativeHandle = nativeHandle;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
