namespace SharpGit2;

public unsafe readonly struct GitRemote(Git2.Remote* handle)
{
    public readonly Git2.Remote* NativeHandle = handle;


}
