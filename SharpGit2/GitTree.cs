using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharpGit2;

public unsafe readonly struct GitTree : IDisposable
{
    internal readonly Git2.Tree* NativeHandle;

    internal GitTree(Git2.Tree* nativeHandle)
    {
        NativeHandle = nativeHandle;
    }

    public GitRepository Owner => new(NativeApi.git_tree_owner(NativeHandle));

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

    public void WalkTree(GitTreeWalkMode mode, Func<string, GitTreeEntry, int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var context = new Git2.ForEachContext<Func<string, GitTreeEntry, int>>() { Callback = callback };
        var gcHandle = GCHandle.Alloc(context, GCHandleType.Normal);
        GitError error;

        try
        {
            error = NativeApi.git_tree_walk(NativeHandle, mode, &_Callback, (nint)gcHandle);
        }
        finally
        {
            gcHandle.Free();
        }

        context.ExceptionInfo?.Throw();
        if (error is < 0 and not GitError.User)
        {
            Git2.ThrowError(error);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int _Callback(byte* pRoot, Git2.TreeEntry* pEntry, nint payload)
        {
            var context = (Git2.ForEachContext<Func<string, GitTreeEntry, int>>)((GCHandle)payload).Target!;

            try
            {
                string root = Utf8StringMarshaller.ConvertToManaged(pRoot)!;
                GitTreeEntry entry = new(pEntry);

                var result = context.Callback(root, entry);

                return result < 0 ? (int)GitError.User : result;
            }
            catch (Exception e)
            {
                Debug.Assert(context.ExceptionInfo is null);
                context.ExceptionInfo = ExceptionDispatchInfo.Capture(e);
                return -1;
            }
        }
    }
}
