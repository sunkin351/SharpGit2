using System.Diagnostics;

using static SharpGit2.GitNativeApi;

namespace SharpGit2;

[DebuggerDisplay("{(NativeHandle is null : (string?)null : GetName())}")]
public unsafe readonly struct GitTreeEntry(Git2.TreeEntry* nativeHandle) : IGitHandle, IComparable<GitTreeEntry>
{
    /// <summary>
    /// The native object pointer
    /// </summary>
    public Git2.TreeEntry* NativeHandle { get; } = nativeHandle;

    public bool IsNull => this.NativeHandle == null;

    public GitFileMode FileMode
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_tree_entry_filemode(handle.NativeHandle);
        }
    }

    public GitFileMode FileModeRaw
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_tree_entry_filemode_raw(handle.NativeHandle);
        }
    }

    public ref readonly GitObjectID Id
    {
        get
        {
            var handle = this.ThrowIfNull();

            return ref *git_tree_entry_id(handle.NativeHandle);
        }
    }

    public GitObjectType ObjectType
    {
        get
        {
            var handle = this.ThrowIfNull();

            return git_tree_entry_type(handle.NativeHandle);
        }
    }

    public void Dispose()
    {
        git_tree_entry_free(this.NativeHandle);
    }

    /// <summary>
    /// Exposed to allow custom handling for null values.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public static int Compare(GitTreeEntry e1, GitTreeEntry e2)
    {
        ArgumentNullException.ThrowIfNull(e1.NativeHandle);
        ArgumentNullException.ThrowIfNull(e2.NativeHandle);

        return git_tree_entry_cmp(e1.NativeHandle, e2.NativeHandle);
    }

    public int CompareTo(GitTreeEntry other)
    {
        if (this.NativeHandle is null | other.NativeHandle is null)
            return ((nint)this.NativeHandle).CompareTo((nint)other.NativeHandle);

        return git_tree_entry_cmp(this.NativeHandle, other.NativeHandle);
    }

    public GitTreeEntry Duplicate()
    {
        var handle = this.ThrowIfNull();

        Git2.TreeEntry* newEntry = null;
        Git2.ThrowIfError(git_tree_entry_dup(&newEntry, handle.NativeHandle));

        return new(newEntry);
    }

    public string GetName()
    {
        var handle = this.ThrowIfNull();

        var nativeName = git_tree_entry_name(handle.NativeHandle);

        return Git2.GetPooledString(nativeName);
    }
}
