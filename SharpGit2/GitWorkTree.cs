namespace SharpGit2;

public unsafe readonly struct GitWorkTree
{
    internal readonly Git2.Worktree* NativeHandle;

    internal GitWorkTree(Git2.Worktree* handle)
    {
        NativeHandle = handle;
    }


}
