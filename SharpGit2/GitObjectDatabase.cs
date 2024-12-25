using static SharpGit2.GitNativeApi;

namespace SharpGit2;

/// <summary>
/// An open object database handle
/// </summary>
/// <param name="nativeHandle">The native object pointer</param>
public unsafe readonly partial struct GitObjectDatabase(Git2.ObjectDatabase* nativeHandle) : IDisposable, IGitHandle
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    public Git2.ObjectDatabase* NativeHandle { get; } = nativeHandle;

    /// <inheritdoc/>
    public bool IsNull => this.NativeHandle == null;

    public void Dispose()
    {
        git_odb_free(NativeHandle);
    }

    public bool ExistsPrefix(in GitObjectID partialId, ushort idPrefixLen, out GitObjectID id, out bool ambiguous)
    {
        var handle = this.ThrowIfNull();

        GitError error;

        fixed (GitObjectID* _partial = &partialId, _id = &id)
        {
            error = git_odb_exists_prefix(_id, handle.NativeHandle, _partial, idPrefixLen);
        }

        switch (error)
        {
            case GitError.OK:
                ambiguous = false;
                return true;
            case GitError.NotFound:
                ambiguous = false;
                return false;
            case GitError.Ambiguous:
                ambiguous = true;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }
}
