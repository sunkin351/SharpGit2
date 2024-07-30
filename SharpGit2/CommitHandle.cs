namespace SharpGit2;

public unsafe readonly struct CommitHandle
{
    internal readonly Git2.Commit* NativeHandle;

    internal CommitHandle(Git2.Commit* handle)
    {
        NativeHandle = handle;
    }
}
