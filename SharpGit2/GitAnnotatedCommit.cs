using static SharpGit2.GitNativeApi;

namespace SharpGit2;

/// <summary>
/// An Annotated commit, the input to merge and rebase.
/// </summary>
/// <param name="nativeHandle">The native object pointer</param>
public unsafe readonly struct GitAnnotatedCommit(Git2.AnnotatedCommit* nativeHandle) : IDisposable, IGitHandle
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    public Git2.AnnotatedCommit* NativeHandle { get; } = nativeHandle;

    public bool IsNull => NativeHandle == null;

    /// <summary>
    /// Frees this Annotated Commit
    /// </summary>
    /// <remarks>
    /// Do not call this twice!
    /// </remarks>
    public void Dispose()
    {
        git_annotated_commit_free(this.NativeHandle);
    }

    /// <summary>
    /// The commit ID that this annotated commit refers to.
    /// </summary>
    public ref readonly GitObjectID Id
    {
        get
        {
            var handle = this.ThrowIfNull();

            return ref *git_annotated_commit_id(handle.NativeHandle);
        }
    }

    /// <summary>
    /// Gets the refname this annotated commit refers to.
    /// </summary>
    /// <returns>The ref name</returns>
    public string? GetRefName()
    {
        var handle = this.ThrowIfNull();

        var nativeName = git_annotated_commit_ref(handle.NativeHandle);
        
        return nativeName is null ? null : Git2.GetPooledString(nativeName);
    }
}
