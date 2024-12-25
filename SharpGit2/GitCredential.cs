using static SharpGit2.GitNativeApi;

namespace SharpGit2;

public unsafe readonly struct GitCredential(Git2.Credential* NativeHandle) : IDisposable, IGitHandle
{
    public Git2.Credential* NativeHandle { get; } = NativeHandle;

    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_credential_free(this.NativeHandle);
    }

    public bool HasUsername
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_credential_has_username(handle.NativeHandle);
        }
    }

    public string? GetUsername()
    {
        var handle = this.ThrowIfNull();

        var nativeName = git_credential_get_username(handle.NativeHandle);

        return nativeName == null ? null : Git2.GetPooledString(nativeName);
    }

    public static GitCredential CreateDefault()
    {
        Git2.Credential* result = null;

        Git2.ThrowIfError(git_credential_default_new(&result));

        return new(result);
    }

    public static GitCredential CreateSSHCustom(string username, ReadOnlySpan<byte> publickey, delegate* unmanaged[Cdecl]<void*, byte**, nuint*, byte*, nuint, void**, int> sign_callback, nint payload)
    {
        Git2.Credential* result = null;

        fixed (byte* _publicKey = publickey)
        {
            Git2.ThrowIfError(git_credential_ssh_custom_new(&result, username, _publicKey, (nuint)publickey.Length, sign_callback, payload));
        }

        return new(result);
    }

    public static GitCredential CreateSSHInteractive(string username, delegate* unmanaged[Cdecl]<byte*, int, byte*, int, int, void*, void*, void**, void> prompt_callback, void* payload)
    {
        Git2.Credential* result = null;

        Git2.ThrowIfError(git_credential_ssh_interactive_new(&result, username, prompt_callback, payload));

        return new(result);
    }

    public static GitCredential CreateSSHKeyFromAgent(string username)
    {
        Git2.Credential* result = null;

        Git2.ThrowIfError(git_credential_ssh_key_from_agent(&result, username));

        return new(result);
    }

    public static GitCredential CreateSSHKeyFromMemory(string username, string? publickey, string privateKey, string? passphrase)
    {
        Git2.Credential* result = null;

        Git2.ThrowIfError(git_credential_ssh_key_memory_new(&result, username, publickey, privateKey, passphrase));

        return new(result);
    }

    public static GitCredential CreateSSHKey(string username, string? publickey, string privateKey, string? passphrase)
    {
        Git2.Credential* result = null;

        Git2.ThrowIfError(git_credential_ssh_key_new(&result, username, publickey, privateKey, passphrase));

        return new(result);
    }

    public static GitCredential CreateUsername(string username)
    {
        Git2.Credential* result = null;

        Git2.ThrowIfError(git_credential_username_new(&result, username));

        return new(result);
    }

    public static GitCredential CreatePlaintext(string username, string password)
    {
        Git2.Credential* result = null;

        Git2.ThrowIfError(git_credential_userpass_plaintext_new(&result, username, password));

        return new(result);
    }
}
