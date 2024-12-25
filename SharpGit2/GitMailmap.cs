using static SharpGit2.GitNativeApi;

namespace SharpGit2;

/// <summary>
/// A mailmap can be used to specify alternate email addresses for repository committers or authors.
/// This allows systems to map commits made using different email addresses to the same logical person.
/// </summary>
/// <param name="nativeHandle">The native object pointer</param>
public unsafe readonly struct GitMailmap(Git2.MailMap* nativeHandle) : IDisposable, IGitHandle
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    public Git2.MailMap* NativeHandle { get; } = nativeHandle;

    /// <inheritdoc/>
    public bool IsNull => this.NativeHandle == null;

    /// <summary>
    /// Frees the GitMailmap object
    /// </summary>
    public void Dispose()
    {
        git_mailmap_free(this.NativeHandle);
    }
}
