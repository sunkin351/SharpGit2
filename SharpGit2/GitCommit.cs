namespace SharpGit2;

public unsafe readonly struct GitCommit
{
    internal readonly Git2.Commit* NativeHandle;

    internal GitCommit(Git2.Commit* handle)
    {
        NativeHandle = handle;
    }
}
