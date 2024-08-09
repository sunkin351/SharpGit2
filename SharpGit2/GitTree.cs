namespace SharpGit2;

public unsafe readonly struct GitTree : IDisposable
{
    internal readonly Git2.Tree* NativeHandle;

    internal GitTree(Git2.Tree* nativeHandle)
    {
        NativeHandle = nativeHandle;
    }

    public RepositoryHandle Owner => new(NativeApi.git_tree_owner(NativeHandle));

    public ref readonly GitObjectID Id => ref *NativeApi.git_tree_id(NativeHandle);

    public nuint NativeEntryCount => NativeApi.git_tree_entrycount(NativeHandle);

    public int EntryCount => checked((int)this.NativeEntryCount);

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public GitTree Duplicate()
    {
        Git2.Tree* tree;
        Git2.ThrowIfError(NativeApi.git_tree_dup(&tree, NativeHandle));

        return new(tree);
    }

    /// <summary>
    /// Lookup a tree entry by it's SHA value
    /// </summary>
    /// <param name="id">The SHA value</param>
    /// <returns>The Tree Entry, or null if one isn't found</returns>
    /// <remarks>
    /// The returned entry is owned by it's parent tree!
    /// Do not dispose! And do not use after disposing the parent tree!
    /// </remarks>
    public GitTreeEntry? GetEntryById(in GitObjectID id)
    {
        Git2.TreeEntry* entry;
        fixed (GitObjectID* pId = &id)
        {
            entry = NativeApi.git_tree_entry_byid(NativeHandle, pId);
        }

        return entry is null ? null : new(entry);
    }

    public GitTreeEntry? GetEntryByIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return GetEntryByIndex((nuint)index);
    }

    /// <summary>
    /// Lookup a tree entry by its position in the tree
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public GitTreeEntry? GetEntryByIndex(nuint index)
    {
        var entry = NativeApi.git_tree_entry_byindex(NativeHandle, index);

        return entry is null ? null : new(entry);
    }

    public GitTreeEntry? GetEntryByName(string filename)
    {
        var entry = NativeApi.git_tree_entry_byname(NativeHandle, filename);

        return entry is null ? null : new(entry);
    }

    public bool TryGetEntryByPath(string path, out GitTreeEntry entry)
    {
        Git2.TreeEntry* tmp;
        var error = NativeApi.git_tree_entry_bypath(&tmp, NativeHandle, path);

        switch (error)
        {
            case GitError.OK:
                entry = new(tmp);
                return true;
            case GitError.NotFound:
                entry = default;
                return false;
            default:
                throw Git2.ExceptionForError(error);
        }
    }
}
