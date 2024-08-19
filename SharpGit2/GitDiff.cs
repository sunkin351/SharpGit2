namespace SharpGit2;

public unsafe readonly struct GitDiff
{
    internal readonly Git2.Diff* NativeHandle;

    internal GitDiff(Git2.Diff* nativeHandle)
    {
        NativeHandle = nativeHandle;
    }
}
