using static SharpGit2.NativeApi;

namespace SharpGit2;

public unsafe readonly struct GitCredential(Git2.Credential* NativeHandle) : IDisposable
{
    public readonly Git2.Credential* NativeHandle = NativeHandle;

    public void Dispose()
    {
        git_credential_free(this.NativeHandle);
    }
}
