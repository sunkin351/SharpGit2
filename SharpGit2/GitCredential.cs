namespace SharpGit2;

public unsafe readonly struct GitCredential(Git2.Credential* NativeHandle)
{
    public readonly Git2.Credential* NativeHandle = NativeHandle;
}
