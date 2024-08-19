namespace SharpGit2;

public unsafe readonly struct GitTransport(Git2.Transport* handle)
{
    public readonly Git2.Transport* NativeHandle = handle;


}