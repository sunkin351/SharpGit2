using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2;

public unsafe readonly struct GitTreeEntry : IDisposable, IComparable<GitTreeEntry>
{
    internal readonly Git2.TreeEntry* NativeHandle;

    internal GitTreeEntry(Git2.TreeEntry* nativeHandle)
    {
        NativeHandle = nativeHandle;
    }

    public GitFileMode FileMode => NativeApi.git_tree_entry_filemode(NativeHandle);

    public GitFileMode FileModeRaw => NativeApi.git_tree_entry_filemode_raw(NativeHandle);

    public ref readonly GitObjectID Id => ref *NativeApi.git_tree_entry_id(NativeHandle);

    public GitObjectType ObjectType => NativeApi.git_tree_entry_type(NativeHandle);

    public void Dispose()
    {
        NativeApi.git_tree_entry_free(NativeHandle);
    }

    /// <summary>
    /// Exposed to allow custom handling for null values.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public static int Compare(GitTreeEntry e1, GitTreeEntry e2)
    {
        ArgumentNullException.ThrowIfNull(e1.NativeHandle);
        ArgumentNullException.ThrowIfNull(e2.NativeHandle);

        return NativeApi.git_tree_entry_cmp(e1.NativeHandle, e2.NativeHandle);
    }

    public int CompareTo(GitTreeEntry other)
    {
        if (NativeHandle is null | other.NativeHandle is null)
            return ((nint)NativeHandle).CompareTo((nint)other.NativeHandle);

        return NativeApi.git_tree_entry_cmp(NativeHandle, other.NativeHandle);
    }

    public GitTreeEntry Duplicate()
    {
        Git2.TreeEntry* newEntry;
        Git2.ThrowIfError(NativeApi.git_tree_entry_dup(&newEntry, NativeHandle));

        return new(newEntry);
    }

    public string GetName()
    {
        return Utf8StringMarshaller.ConvertToManaged(NativeApi.git_tree_entry_name(NativeHandle))!;
    }
}
